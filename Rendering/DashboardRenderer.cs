using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using HyteY70.Sensors;

namespace HyteY70.Rendering;

public sealed class DashboardRenderer : IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly bool _mediaMode;
    private readonly Image? _media;
    private readonly FrameDimension? _dimension;
    private readonly int _frameCount;
    private readonly int[] _durations;
    private readonly int _loopMs;
    private readonly int _rotate;
    private readonly bool _flipH;
    private readonly bool _flipV;
    private readonly bool _coverFit;
    private readonly double _zoom;
    private readonly bool _showClock;
    private readonly bool _showCpu;
    private readonly bool _showGpu;
    private readonly bool _showRam;
    private readonly bool _overlayStats;
    private readonly Color _letterbox;
    private readonly Font _clockFont;
    private readonly Font _titleFont;
    private readonly Font _valueFont;
    private readonly Font _subFont;
    private readonly Font _footerFont;
    private readonly Bitmap _canvas;
    private readonly Graphics _graphics;

    public bool MediaMode => _mediaMode;

    public int GetMediaFrameIndex(TimeSpan elapsed) => FrameIndex(elapsed);

    public int GetMediaFrameDurationMs(int index) =>
        index >= 0 && index < _durations.Length ? _durations[index] : 80;

    public DashboardRenderer(int width, int height, AppConfig config, string? mediaPath)
    {
        _width = width;
        _height = height;
        _rotate = ((config.Rotate % 360) + 360) % 360;
        _flipH = config.FlipHorizontal;
        _flipV = config.FlipVertical;
        _coverFit = config.UseCoverFit;
        _zoom = Math.Clamp(config.Zoom <= 0 ? 1.0 : config.Zoom, 0.1, 5.0);
        _showClock = config.ShowClock;
        _showCpu = config.ShowCpu;
        _showGpu = config.ShowGpu;
        _showRam = config.ShowRam;
        _overlayStats = config.OverlayStats;
        _letterbox = config.ParseLetterboxColor();

        _clockFont = new Font("Segoe UI", 28, FontStyle.Regular, GraphicsUnit.Pixel);
        _titleFont = new Font("Segoe UI Semibold", 28, FontStyle.Bold, GraphicsUnit.Pixel);
        _valueFont = new Font("Segoe UI Semibold", 96, FontStyle.Bold, GraphicsUnit.Pixel);
        _subFont = new Font("Segoe UI", 24, FontStyle.Regular, GraphicsUnit.Pixel);
        _footerFont = new Font("Segoe UI", 22, FontStyle.Regular, GraphicsUnit.Pixel);

        _canvas = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        _graphics = Graphics.FromImage(_canvas);
        _graphics.CompositingQuality = CompositingQuality.HighSpeed;
        _graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

        if (config.UseMedia(mediaPath) && mediaPath is not null)
        {
            _media = Image.FromFile(mediaPath);
            _dimension = new FrameDimension(_media.FrameDimensionsList[0]);
            _frameCount = Math.Max(1, _media.GetFrameCount(_dimension));
            _durations = new int[_frameCount];
            for (int i = 0; i < _frameCount; i++)
            {
                int duration = 80;
                try
                {
                    _media.SelectActiveFrame(_dimension, i);
                    var item = _media.GetPropertyItem(0x5100);
                    if (item.Value is { Length: >= 4 })
                        duration = Math.Max(20, BitConverter.ToInt32(item.Value, i * 4) * 10);
                }
                catch { }
                _durations[i] = duration;
            }
            _loopMs = Math.Max(1, _durations.Sum());
            _mediaMode = true;
            _graphics.CompositingMode = CompositingMode.SourceCopy;
            _graphics.InterpolationMode = InterpolationMode.Bilinear;
            _graphics.SmoothingMode = SmoothingMode.HighSpeed;
        }
        else
        {
            _mediaMode = false;
            _frameCount = 0;
            _durations = Array.Empty<int>();
            _loopMs = 1;
            _graphics.CompositingMode = CompositingMode.SourceOver;
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _graphics.InterpolationMode = InterpolationMode.Bilinear;
        }
    }

    public Bitmap Render(Stats stats, TimeSpan elapsed, string clock)
    {
        var g = _graphics;
        g.ResetTransform();

        if (_mediaMode && _media is not null && _dimension is not null)
        {
            int index = FrameIndex(elapsed);
            _media.SelectActiveFrame(_dimension, index);
            DrawMedia(g, _media);
            if (_overlayStats)
            {
                g.ResetTransform();
                g.CompositingMode = CompositingMode.SourceOver;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                DrawOverlay(g, stats, clock);
            }
        }
        else
        {
            DrawStatsBackground(g, elapsed);
            DrawCards(g, stats, clock);
        }

        return (Bitmap)_canvas.Clone();
    }

    public void Dispose()
    {
        _graphics.Dispose();
        _canvas.Dispose();
        _media?.Dispose();
        _clockFont.Dispose();
        _titleFont.Dispose();
        _valueFont.Dispose();
        _subFont.Dispose();
        _footerFont.Dispose();
    }

    private int FrameIndex(TimeSpan elapsed)
    {
        if (_frameCount <= 1) return 0;
        int cursor = (int)(elapsed.TotalMilliseconds % _loopMs);
        int acc = 0;
        for (int i = 0; i < _frameCount; i++)
        {
            acc += _durations[i];
            if (cursor < acc) return i;
        }
        return _frameCount - 1;
    }

    private void DrawMedia(Graphics g, Image source)
    {
        ApplyOrientationTransform(g);
        bool swap = _rotate is 90 or 270;
        int targetW = swap ? _height : _width;
        int targetH = swap ? _width : _height;
        double baseScale = _coverFit
            ? Math.Max(targetW / (double)source.Width, targetH / (double)source.Height)
            : Math.Min(targetW / (double)source.Width, targetH / (double)source.Height);
        double scale = baseScale * _zoom;
        int w = Math.Max(1, (int)(source.Width * scale));
        int h = Math.Max(1, (int)(source.Height * scale));
        int x = (targetW - w) / 2;
        int y = (targetH - h) / 2;
        g.Clear(_letterbox);
        g.DrawImage(source, new Rectangle(x, y, w, h));
    }

    private void ApplyOrientationTransform(Graphics g)
    {
        g.TranslateTransform(_width / 2f, _height / 2f);
        if (_rotate == 90) g.RotateTransform(90);
        else if (_rotate == 180) g.RotateTransform(180);
        else if (_rotate == 270) g.RotateTransform(270);
        if (_flipH || _flipV)
            g.ScaleTransform(_flipH ? -1 : 1, _flipV ? -1 : 1);
        bool swap = _rotate is 90 or 270;
        float halfW = (swap ? _height : _width) / 2f;
        float halfH = (swap ? _width : _height) / 2f;
        g.TranslateTransform(-halfW, -halfH);
    }

    private void DrawStatsBackground(Graphics g, TimeSpan elapsed)
    {
        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, _width, _height),
            Color.FromArgb(6, 10, 20),
            Color.FromArgb(18, 24, 48),
            90f);
        g.FillRectangle(brush, 0, 0, _width, _height);
        double t = elapsed.TotalSeconds;
        DrawBlob(g, _width * 0.5 + Math.Cos(t * 0.22) * _width * 0.22, _height * 0.25 + Math.Sin(t * 0.18) * _height * 0.08, _width * 0.7, Color.FromArgb(120, 36, 92, 190));
        DrawBlob(g, _width * 0.35 + Math.Cos(t * 0.29 + 1) * _width * 0.15, _height * 0.55 + Math.Sin(t * 0.21) * _height * 0.1, _width * 0.55, Color.FromArgb(110, 150, 54, 170));
        DrawBlob(g, _width * 0.7 + Math.Cos(t * 0.17 + 2) * _width * 0.12, _height * 0.78 + Math.Sin(t * 0.25) * _height * 0.08, _width * 0.45, Color.FromArgb(100, 20, 150, 160));
    }

    private void DrawCards(Graphics g, Stats stats, string clock)
    {
        const int margin = 72;
        const int gap = 28;
        const int cardH = 300;
        int cardW = _width - margin * 2;

        if (_showClock)
        {
            using var clockBrush = new SolidBrush(Color.FromArgb(230, 235, 240, 248));
            g.DrawString(clock, _clockFont, clockBrush, margin, 56);
        }

        var cards = new List<(string Title, string Value, string Sub, Color Accent)>();
        if (_showCpu)
        {
            string cpuSub = stats.CpuMhz is null ? "carga" : $"{stats.CpuMhz.Value / 1000:0.00} GHz";
            if (stats.CpuTemp is not null) cpuSub = $"{stats.CpuTemp:0}°   ·   {cpuSub}";
            cards.Add(("CPU", Text(stats.CpuPercent, "%"), cpuSub, Color.FromArgb(126, 196, 255)));
        }
        if (_showGpu)
            cards.Add(("GPU", Text(stats.GpuTemp, "°"), $"hot {Text(stats.GpuHotspot, "°")}   ·   uso {Text(stats.GpuPercent, "%")}", Color.FromArgb(255, 176, 92)));
        if (_showRam)
            cards.Add(("RAM", Text(stats.RamPercent, "%"), "en uso", Color.FromArgb(126, 230, 190)));

        int top = (_height - (cardH * Math.Max(1, cards.Count) + gap * Math.Max(0, cards.Count - 1))) / 2;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            DrawCard(g, new Rectangle(margin, top + i * (cardH + gap), cardW, cardH), card.Title, card.Value, card.Sub, card.Accent);
        }

        using var footerBrush = new SolidBrush(Color.FromArgb(200, 180, 190, 205));
        g.DrawString(stats.GpuName, _footerFont, footerBrush, margin, _height - 86);
    }

    private void DrawOverlay(Graphics g, Stats stats, string clock)
    {
        const int pad = 36;
        using var bg = new SolidBrush(Color.FromArgb(150, 8, 12, 22));
        g.FillRectangle(bg, 0, 0, _width, 120);
        using var text = new SolidBrush(Color.FromArgb(240, 235, 240, 248));
        string line = clock;
        if (_showCpu) line += $"   CPU {Text(stats.CpuPercent, "%")}" + (stats.CpuTemp is null ? "" : $" {stats.CpuTemp:0}°");
        if (_showGpu) line += $"   GPU {Text(stats.GpuTemp, "°")}";
        if (_showRam) line += $"   RAM {Text(stats.RamPercent, "%")}";
        g.DrawString(line, _subFont, text, pad, 42);
    }

    private void DrawCard(Graphics g, Rectangle box, string title, string value, string subtitle, Color accent)
    {
        using var fill = new SolidBrush(Color.FromArgb(168, 8, 12, 22));
        using var path = RoundRect(box, 32);
        g.FillPath(fill, path);
        using var titleBrush = new SolidBrush(accent);
        using var valueBrush = new SolidBrush(Color.FromArgb(245, 248, 252));
        using var subBrush = new SolidBrush(Color.FromArgb(230, 190, 198, 210));
        g.DrawString(title, _titleFont, titleBrush, box.X + 40, box.Y + 28);
        g.DrawString(value, _valueFont, valueBrush, box.X + 32, box.Y + 78);
        g.DrawString(subtitle, _subFont, subBrush, box.X + 42, box.Y + 220);
    }

    private static void DrawBlob(Graphics g, double cx, double cy, double diameter, Color color)
    {
        int d = Math.Max(8, (int)diameter);
        var rect = new Rectangle((int)(cx - d / 2.0), (int)(cy - d / 2.0), d, d);
        using var path = new GraphicsPath();
        path.AddEllipse(rect);
        using var brush = new PathGradientBrush(path)
        {
            CenterColor = color,
            SurroundColors = new[] { Color.FromArgb(0, color) }
        };
        g.FillEllipse(brush, rect);
    }

    private static GraphicsPath RoundRect(Rectangle box, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(box.X, box.Y, d, d, 180, 90);
        path.AddArc(box.Right - d, box.Y, d, d, 270, 90);
        path.AddArc(box.Right - d, box.Bottom - d, d, d, 0, 90);
        path.AddArc(box.X, box.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static string Text(double? value, string suffix) =>
        value is null ? "--" : suffix == "%" ? $"{value:0}%" : $"{value:0}{suffix}";
}

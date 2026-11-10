using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HyteY70.Rendering;
using HyteY70.Sensors;

namespace HyteY70;

public partial class MainWindow : Window
{
    private readonly StatsReader _stats = new();
    private readonly DispatcherTimer _timer;
    private readonly DateTime _started = DateTime.UtcNow;
    private readonly WriteableBitmap _target;
    private readonly int _width;
    private readonly int _height;
    private readonly string _baseDir;
    private DashboardRenderer _renderer;
    private AppConfig _config;
    private List<string> _playlist = new();
    private int _slideIndex;
    private int _lastMediaFrame = -1;

    public MainWindow(MonitorBounds bounds, AppConfig config, string baseDir)
    {
        InitializeComponent();
        MonitorFinder.Place(this, bounds);
        _width = bounds.Width;
        _height = bounds.Height;
        _baseDir = baseDir;
        _config = config;
        _renderer = CreateRenderer(config);
        _target = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgra32, null);
        Canvas.Source = _target;
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = FrameInterval(config.Fps)
        };
        _timer.Tick += (_, _) => Tick();
        Loaded += (_, _) =>
        {
            Paint(force: true);
            _timer.Start();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _renderer.Dispose();
            _stats.Dispose();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape)
                HideToTray();
        };
    }

    public string StatusText
    {
        get
        {
            if (!_renderer.MediaMode) return "Estadísticas";
            string slide = _playlist.Count > 1
                ? $" {_slideIndex + 1}/{_playlist.Count}"
                : "";
            return $"Media{slide} ({_config.Rotate}° {_config.Fit} x{_config.Zoom:0.##})";
        }
    }

    public void ShowOnDisplay()
    {
        Show();
        Activate();
    }

    public void HideToTray() => Hide();

    public void ApplyConfig(AppConfig config)
    {
        _config = config;
        var old = _renderer;
        _renderer = CreateRenderer(config);
        old.Dispose();
        _lastMediaFrame = -1;
        _timer.Interval = FrameInterval(config.Fps);
        Paint(force: true);
    }

    private DashboardRenderer CreateRenderer(AppConfig config)
    {
        _playlist = config.ResolvePlaylist(_baseDir);
        if (_playlist.Count == 0)
            return new DashboardRenderer(_width, _height, config, null);

        if (config.SlideSeconds > 0 && _playlist.Count > 1)
        {
            int period = Math.Max(1, config.SlideSeconds);
            _slideIndex = (int)((DateTime.UtcNow - _started).TotalSeconds / period) % _playlist.Count;
        }
        else
            _slideIndex = Math.Clamp(_slideIndex, 0, _playlist.Count - 1);

        string media = _playlist[_slideIndex];
        string? path = config.UseMedia(media) ? media : null;
        return new DashboardRenderer(_width, _height, config, path);
    }

    private bool TryAdvanceSlide()
    {
        if (_config.SlideSeconds <= 0 || _playlist.Count <= 1) return false;
        int period = Math.Max(1, _config.SlideSeconds);
        int next = (int)((DateTime.UtcNow - _started).TotalSeconds / period) % _playlist.Count;
        if (next == _slideIndex) return false;

        _slideIndex = next;
        var old = _renderer;
        _renderer = new DashboardRenderer(
            _width,
            _height,
            _config,
            _config.UseMedia(_playlist[_slideIndex]) ? _playlist[_slideIndex] : null);
        old.Dispose();
        _lastMediaFrame = -1;
        return true;
    }

    private static TimeSpan FrameInterval(int fps) =>
        TimeSpan.FromMilliseconds(Math.Max(16, 1000.0 / Math.Max(1, fps)));

    private void Tick()
    {
        if (TryAdvanceSlide())
        {
            _timer.Interval = FrameInterval(_config.Fps);
            Paint(force: true);
            return;
        }

        var elapsed = DateTime.UtcNow - _started;
        if (_renderer.MediaMode)
        {
            int index = _renderer.GetMediaFrameIndex(elapsed);
            int frameMs = _renderer.GetMediaFrameDurationMs(index);
            if (index == _lastMediaFrame)
            {
                int idleFps = Math.Max(0, _config.FpsIdle);
                // Keep polling often enough to catch the next slideshow switch.
                int waitMs = idleFps <= 0
                    ? Math.Max(16, frameMs)
                    : Math.Max(16, 1000 / Math.Max(1, idleFps));
                if (_config.SlideSeconds > 0 && _playlist.Count > 1)
                    waitMs = Math.Min(waitMs, 1000);
                _timer.Interval = TimeSpan.FromMilliseconds(waitMs);
                return;
            }
            _lastMediaFrame = index;
            _timer.Interval = TimeSpan.FromMilliseconds(Math.Min(
                Math.Max(16, 1000.0 / Math.Max(1, _config.Fps)),
                Math.Max(16, frameMs)));
        }
        else
        {
            _timer.Interval = FrameInterval(_config.Fps);
        }
        Paint(force: false);
    }

    private void Paint(bool force)
    {
        using var bitmap = _renderer.Render(_stats.Sample(), DateTime.UtcNow - _started, DateTime.Now.ToString("HH:mm"));
        var data = bitmap.LockBits(
            new System.Drawing.Rectangle(0, 0, _width, _height),
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            _target.Lock();
            try
            {
                int bytes = data.Stride * _height;
                unsafe
                {
                    Buffer.MemoryCopy((void*)data.Scan0, (void*)_target.BackBuffer, bytes, bytes);
                }
                _target.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            }
            finally
            {
                _target.Unlock();
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}

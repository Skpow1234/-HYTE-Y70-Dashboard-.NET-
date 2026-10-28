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
        _timer.Tick += (_, _) => Paint();
        Loaded += (_, _) =>
        {
            Paint();
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

    public string StatusText =>
        _renderer.MediaMode
            ? $"Media ({_config.Rotate}° {_config.Fit} x{_config.Zoom:0.##})"
            : "Estadísticas";

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
        _timer.Interval = FrameInterval(config.Fps);
        Paint();
    }

    private DashboardRenderer CreateRenderer(AppConfig config)
    {
        string? media = config.ResolveMedia(_baseDir);
        string? path = config.UseMedia(media) ? media : null;
        return new DashboardRenderer(_width, _height, config, path);
    }

    private static TimeSpan FrameInterval(int fps) =>
        TimeSpan.FromMilliseconds(Math.Max(16, 1000.0 / Math.Max(1, fps)));

    private void Paint()
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

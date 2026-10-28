using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace HyteY70;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private Forms.NotifyIcon? _tray;
    private MainWindow? _window;
    private ConfigWatcher? _watcher;
    private string _baseDir = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _mutex = new Mutex(true, @"Local\HyteY70Dashboard", out bool created);
        if (!created)
        {
            Forms.MessageBox.Show("HYTE Y70 ya está en ejecución (icono de la bandeja).", "HYTE Y70");
            Shutdown();
            return;
        }

        _baseDir = AppContext.BaseDirectory;
        var config = AppConfig.Load(_baseDir);
        StartupHelper.Apply(config.RunAtLogin, Path.Combine(_baseDir, "HyteY70.exe"));
        _watcher = new ConfigWatcher(_baseDir, () => Dispatcher.Invoke(ReloadConfig), config.WatchConfig);

        MonitorBounds bounds;
        try
        {
            bounds = MonitorFinder.FindY70();
        }
        catch (Exception ex)
        {
            Forms.MessageBox.Show(ex.Message, "HYTE Y70");
            Shutdown();
            return;
        }

        _window = new MainWindow(bounds, config, _baseDir);
        if (config.StartHidden)
            _window.HideToTray();
        else
            _window.ShowOnDisplay();

        _tray = new Forms.NotifyIcon
        {
            Visible = true,
            Text = "HYTE Y70 Dashboard",
            Icon = SystemIcons.Application,
            ContextMenuStrip = BuildMenu()
        };
        _tray.DoubleClick += (_, _) => _window?.ShowOnDisplay();
        _tray.ShowBalloonTip(2500, "HYTE Y70", "Corriendo en segundo plano. Doble clic en la bandeja para mostrar.", Forms.ToolTipIcon.Info);
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mostrar en Y70", null, (_, _) => _window?.ShowOnDisplay());
        menu.Items.Add("Ocultar (sigue en bandeja)", null, (_, _) => _window?.HideToTray());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Recargar config", null, (_, _) => ReloadConfig());
        menu.Items.Add("Abrir carpeta de config", null, (_, _) => StartupHelper.OpenConfigFolder(_baseDir));
        menu.Items.Add(StartupHelper.IsEnabled() ? "No iniciar con Windows" : "Iniciar con Windows", null, (_, _) => ToggleLogin());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitApp());
        return menu;
    }

    private void ReloadConfig()
    {
        try
        {
            var config = AppConfig.Load(_baseDir);
            StartupHelper.Apply(config.RunAtLogin, Path.Combine(_baseDir, "HyteY70.exe"));
            _watcher?.Dispose();
            _watcher = new ConfigWatcher(_baseDir, () => Dispatcher.Invoke(ReloadConfig), config.WatchConfig);
            _window?.ApplyConfig(config);
            _tray!.ContextMenuStrip = BuildMenu();
            _tray.ShowBalloonTip(2500, "HYTE Y70", $"Config recargada — {_window?.StatusText}", Forms.ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            Forms.MessageBox.Show(ex.Message, "HYTE Y70");
        }
    }

    private void ToggleLogin()
    {
        bool next = !StartupHelper.IsEnabled();
        StartupHelper.Apply(next, Path.Combine(_baseDir, "HyteY70.exe"));
        _tray!.ContextMenuStrip = BuildMenu();
        _tray.ShowBalloonTip(2000, "HYTE Y70", next ? "Se iniciará con Windows." : "Ya no inicia con Windows.", Forms.ToolTipIcon.Info);
    }

    private void ExitApp()
    {
        _watcher?.Dispose();
        _tray!.Visible = false;
        _tray.Dispose();
        _window?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _watcher?.Dispose();
        _tray?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using LightCopyTranslator.Services;
using LightCopyTranslator.ViewModels;

namespace LightCopyTranslator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly TimeSpan _doubleCopyWindow = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan _minDoubleCopyInterval = TimeSpan.FromMilliseconds(60);
    private ClipboardMonitor? _clipboard;
    private TrayService? _tray;
    private MainWindow? _window;
    private SettingsWindow? _settingsWindow;
    private AppConfig? _config;
    private DateTime _lastCopyAt = DateTime.MinValue;
    private string? _lastText;
    private bool _exitRequested;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterExceptionHandlers();

        try
        {
            var config = ConfigService.Load();
            _config = config;
            var translators = BuildTranslators(config);

            var viewModel = new TranslationViewModel(translators, Dispatcher);

            _window = new MainWindow();
            _window.Initialize(viewModel);
            _window.ApplyShowSourcePanel(config.Ui.ShowSourcePanel);
            MainWindow = _window;

            _tray = new TrayService();
            _tray.ShowRequested += ToggleWindow;
            _tray.SettingsRequested += OpenSettings;
            _tray.ExitRequested += OnExitRequested;

            _clipboard = new ClipboardMonitor();
            _clipboard.ClipboardTextChanged += OnClipboardTextChanged;

            EnsureWindowHandle();
            LogInfo("Startup OK");
        }
        catch (Exception ex)
        {
            LogError("Startup failed", ex);
            System.Windows.MessageBox.Show($"启动失败：{ex.Message}\n日志：{GetLogPath()}", "Light Copy Translator",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private void ToggleWindow()
    {
        if (_window == null)
        {
            return;
        }

        if (_window.IsVisible)
        {
            _window.Hide();
        }
        else
        {
            _window.ShowAtCursor();
        }
    }

    private void OnClipboardTextChanged(object? sender, string text)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (_lastText == trimmed)
        {
            var delta = now - _lastCopyAt;
            if (delta < _minDoubleCopyInterval)
            {
                return;
            }

            if (delta <= _doubleCopyWindow)
            {
                _lastText = null;
                _lastCopyAt = DateTime.MinValue;
                _window?.ShowForText(trimmed);
                return;
            }
        }

        _lastText = trimmed;
        _lastCopyAt = now;
    }

    private void OnExitRequested()
    {
        if (_exitRequested)
        {
            return;
        }

        _exitRequested = true;
        _clipboard?.Dispose();
        _tray?.Dispose();

        if (_window != null)
        {
            _window.AllowClose();
            _window.Close();
        }

        Shutdown();
    }

    internal void SuppressNextClipboardNotification()
    {
        _clipboard?.SuppressNext();
    }

    private void OpenSettings()
    {
        if (_config == null)
        {
            return;
        }

        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_config);
        _settingsWindow.SettingsSaved += ApplySettings;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ApplySettings()
    {
        if (_config == null)
        {
            return;
        }

        _window?.ApplyShowSourcePanel(_config.Ui.ShowSourcePanel);
        if (_window != null)
        {
            var translators = BuildTranslators(_config);
            var viewModel = new TranslationViewModel(translators, Dispatcher);
            _window.Initialize(viewModel);
        }
    }

    private static ITranslator[] BuildTranslators(AppConfig config)
    {
        var translators = config.Models
            .Where(model => model.Enabled)
            .Select(model => new ModelTranslator(model))
            .Cast<ITranslator>()
            .ToArray();

        if (translators.Length == 0)
        {
            translators =
            [
                new ModelTranslator(new ModelConfig
                {
                    Name = "DeepSeek",
                    Model = "deepseek/deepseek-chat"
                })
            ];
        }

        return translators;
    }

    private static void RegisterExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                LogError("Unhandled exception", ex);
            }
        };

        Current.DispatcherUnhandledException += (_, args) =>
        {
            LogError("Dispatcher unhandled exception", args.Exception);
            System.Windows.MessageBox.Show($"发生未处理异常：{args.Exception.Message}\n日志：{GetLogPath()}", "Light Copy Translator",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    private static void LogInfo(string message)
    {
        WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    private static void LogError(string title, Exception ex)
    {
        WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}\n{ex}");
    }

    private static void WriteLog(string text)
    {
        try
        {
            var path = GetLogPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.AppendAllText(path, text + Environment.NewLine);
        }
        catch
        {
            // Ignore logging failures.
        }
    }

    private static string GetLogPath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseDir, "LightCopyTranslator", "startup.log");
    }

    private void EnsureWindowHandle()
    {
        if (_window == null)
        {
            return;
        }

        var helper = new WindowInteropHelper(_window);
        _ = helper.EnsureHandle();
    }
}

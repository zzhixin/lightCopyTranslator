using System;
using System.Linq;
using System.Windows;
using LightCopyTranslator.Services;
using LightCopyTranslator.ViewModels;

namespace LightCopyTranslator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly TimeSpan _doubleCopyWindow = TimeSpan.FromMilliseconds(500);
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

        var config = ConfigService.Load();
        _config = config;
        var translators = BuildTranslators(config);

        var viewModel = new TranslationViewModel(translators, Dispatcher);

        _window = new MainWindow();
        _window.Initialize(viewModel);
        _window.ApplyShowSourcePanel(config.Ui.ShowSourcePanel);

        _tray = new TrayService();
        _tray.ShowRequested += ToggleWindow;
        _tray.SettingsRequested += OpenSettings;
        _tray.ExitRequested += OnExitRequested;

        _clipboard = new ClipboardMonitor();
        _clipboard.ClipboardTextChanged += OnClipboardTextChanged;
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
        if (_lastText == trimmed && (now - _lastCopyAt) <= _doubleCopyWindow)
        {
            _lastText = null;
            _lastCopyAt = DateTime.MinValue;
            _window?.ShowForText(trimmed);
            return;
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
}

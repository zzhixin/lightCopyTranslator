using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LightCopyTranslator.Services;

internal sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public event Action? ShowRequested;
    public event Action? ExitRequested;
    public event Action? SettingsRequested;

    public TrayService()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon() ?? SystemIcons.Application,
            Text = "Light Copy Translator",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("显示/隐藏");
        toggleItem.Click += (_, _) => ShowRequested?.Invoke();
        menu.Items.Add(toggleItem);

        var settingsItem = new ToolStripMenuItem("设置");
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke();
        menu.Items.Add(settingsItem);

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private static Icon? LoadTrayIcon()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var iconPath = Path.Combine(baseDir, "Resources", "tray.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // Fallback to default icon.
        }

        return null;
    }
}

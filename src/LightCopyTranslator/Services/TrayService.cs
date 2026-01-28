using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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
            var pngPath = Path.Combine(baseDir, "resource", "icon.png");
            if (File.Exists(pngPath))
            {
                using var bitmap = new Bitmap(pngPath);
                var hIcon = bitmap.GetHicon();
                try
                {
                    using var icon = Icon.FromHandle(hIcon);
                    return (Icon)icon.Clone();
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }

            var icoPath = Path.Combine(baseDir, "resource", "tray.ico");
            if (File.Exists(icoPath))
            {
                return new Icon(icoPath);
            }
        }
        catch
        {
            // Fallback to default icon.
        }

        return null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LightCopyTranslator.Services;

internal sealed class ClipboardMonitor : IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    private const int HWND_MESSAGE = -3;

    private readonly HwndSource _source;
    private bool _disposed;
    private int _suppressCount;

    public event EventHandler<string>? ClipboardTextChanged;

    public ClipboardMonitor()
    {
        var parameters = new HwndSourceParameters("LightCopyTranslator.Clipboard")
        {
            Width = 0,
            Height = 0,
            ParentWindow = new IntPtr(HWND_MESSAGE)
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
        AddClipboardFormatListener(_source.Handle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            TryNotifyClipboardText();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void TryNotifyClipboardText()
    {
        if (_suppressCount > 0)
        {
            _suppressCount--;
            return;
        }

        if (!System.Windows.Clipboard.ContainsText())
        {
            return;
        }

        try
        {
            var text = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.UnicodeText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                ClipboardTextChanged?.Invoke(this, text);
            }
        }
        catch
        {
            // Clipboard may be busy; ignore this update.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        RemoveClipboardFormatListener(_source.Handle);
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    public void SuppressNext(int count = 1)
    {
        if (count < 1)
        {
            return;
        }

        _suppressCount += count;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
}

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using LightCopyTranslator.ViewModels;

namespace LightCopyTranslator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private TranslationViewModel? _viewModel;
    private bool _allowClose;
    private double _fullWidth;
    private double _lastGridWidth;
    private double _lastRightColumnWidth;
    private bool _pendingApplyShowSource;
    private bool _pendingShowSourcePanel;
    private DateTime _suppressDeactivateUntil = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    internal void Initialize(TranslationViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public void ShowForText(string text)
    {
        if (_viewModel == null)
        {
            return;
        }

        ShowAtCursor();
        _ = _viewModel.TranslateAsync(text);
    }

    public void ShowAtCursor()
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var point = transform.Transform(new System.Windows.Point(cursor.X, cursor.Y));

        var workArea = SystemParameters.WorkArea;
        var margin = 12.0;

        var left = point.X + margin;
        var top = point.Y + margin;

        if (left + Width > workArea.Right)
        {
            left = workArea.Right - Width - margin;
        }

        if (top + Height > workArea.Bottom)
        {
            top = workArea.Bottom - Height - margin;
        }

        if (left < workArea.Left)
        {
            left = workArea.Left + margin;
        }

        if (top < workArea.Top)
        {
            top = workArea.Top + margin;
        }

        Left = left;
        Top = top;

        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
        _suppressDeactivateUntil = DateTime.UtcNow.AddMilliseconds(400);
        ForceForeground();
    }

    public void AllowClose()
    {
        _allowClose = true;
    }

    public void ApplyShowSourcePanel(bool show)
    {
        _pendingShowSourcePanel = show;
        if (show)
        {
            SourceColumn.Width = new GridLength(0.45, GridUnitType.Star);
            ResultColumn.Width = new GridLength(0.55, GridUnitType.Star);
            SourcePanel.Visibility = Visibility.Visible;
            ResultPanel.Margin = new Thickness(8, 0, 0, 0);
        }
        else
        {
            SourceColumn.Width = new GridLength(0);
            ResultColumn.Width = new GridLength(1, GridUnitType.Star);
            SourcePanel.Visibility = Visibility.Collapsed;
            ResultPanel.Margin = new Thickness(0);
        }

        if (!IsLoaded)
        {
            _pendingApplyShowSource = true;
            return;
        }

        UpdateWindowWidth(show);
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (DateTime.UtcNow < _suppressDeactivateUntil)
        {
            return;
        }

        if (IsVisible)
        {
            Hide();
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void CopyTranslation_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        if (sender is not FrameworkElement element || element.DataContext is not TranslationCard card)
        {
            return;
        }

        if (card.IsLoading || string.IsNullOrWhiteSpace(card.Text))
        {
            return;
        }

        if (System.Windows.Application.Current is App app)
        {
            app.SuppressNextClipboardNotification();
        }

        System.Windows.Clipboard.SetText(card.Text.Trim());
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_pendingApplyShowSource)
        {
            _pendingApplyShowSource = false;
            UpdateWindowWidth(_pendingShowSourcePanel);
        }
    }

    private void UpdateWindowWidth(bool show)
    {
        if (show)
        {
            if (_fullWidth > 0)
            {
                Width = _fullWidth;
            }

            _lastGridWidth = RootGrid.ActualWidth;
            if (_lastGridWidth > 0)
            {
                _lastRightColumnWidth = _lastGridWidth * 0.55 / (0.45 + 0.55);
            }

            return;
        }

        if (_fullWidth <= 0)
        {
            _fullWidth = Width;
        }

        if (_lastGridWidth <= 0)
        {
            _lastGridWidth = RootGrid.ActualWidth;
        }

        var margin = RootGrid.Margin.Left + RootGrid.Margin.Right;
        var chrome = _fullWidth - (_lastGridWidth + margin);
        if (chrome < 0)
        {
            chrome = 0;
        }

        var rightWidth = _lastRightColumnWidth;
        if (rightWidth <= 0)
        {
            var gridWidth = _lastGridWidth > 0 ? _lastGridWidth : Math.Max(0, _fullWidth - margin - chrome);
            rightWidth = gridWidth * 0.55 / (0.45 + 0.55);
        }

        var desired = chrome + margin + rightWidth;
        if (desired > 0)
        {
            Width = desired;
        }
    }

    private void ForceForeground()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var foreground = GetForegroundWindow();
        var currentThreadId = GetCurrentThreadId();
        var foregroundThreadId = GetWindowThreadProcessId(foreground, out _);
        var attached = false;

        if (foregroundThreadId != currentThreadId && foregroundThreadId != 0)
        {
            attached = AttachThreadInput(foregroundThreadId, currentThreadId, true);
        }

        ShowWindow(hwnd, SW_SHOW);
        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);

        if (attached)
        {
            AttachThreadInput(foregroundThreadId, currentThreadId, false);
        }
    }

    private const int SW_SHOW = 5;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

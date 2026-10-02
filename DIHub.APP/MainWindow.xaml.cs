using System;
using System.Runtime.InteropServices;
using DIHub.APP.Views;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DIHub.APP
{
    public sealed partial class MainWindow : Window
    {
        private readonly IThemeService _theme;
        private readonly ISettingsService _settings;
        private bool _isClosing;

        // Absolute minimum window size. Below this the title bar cannot
        // fit its icons + caption buttons without overlap.
        private const int MinWindowWidth = 520;
        private const int MinWindowHeight = 400;

        // Win32 subclassing for WM_GETMINMAXINFO (enforces minimum size).
        private IntPtr _hwnd;
        private SUBCLASSPROC? _subclassProc;

        public MainWindow()
        {
            InitializeComponent();

            _theme = App.GetService<IThemeService>();
            _settings = App.GetService<ISettingsService>();

            _theme.ThemeChanged += OnThemeChanged;

            ConfigureTitleBar();
            ConfigureWindow();
            SetWindowIcon();
            RestoreWindowState();

            // Install the WM_GETMINMAXINFO hook so the OS enforces our
            // minimum size. Must run after the window handle exists.
            DispatcherQueue.TryEnqueue(InstallMinSizeHook);

            MainViewContent.TitleBarReady += OnTitleBarReady;
            MainViewContent.Loaded += OnMainViewLoaded;
            MainViewContent.SizeChanged += (s, e) => UpdateTitleBarInset();

            AppWindow.Closing += OnAppWindowClosing;
        }

        // ─────────────────────────────────────────────
        //  Minimum window size via Win32
        //  ─────────────────────────────────────────────

        private delegate IntPtr SUBCLASSPROC(
            IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
            IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(
            IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(
            IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private const uint WM_GETMINMAXINFO = 0x0024;

        private void InstallMinSizeHook()
        {
            try
            {
                _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (_hwnd == IntPtr.Zero) return;

                _subclassProc = MinSizeSubclassProc;
                SetWindowSubclass(_hwnd, _subclassProc, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        private IntPtr MinSizeSubclassProc(
            IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
            IntPtr uIdSubclass, IntPtr dwRefData)
        {
            try
            {
                if (uMsg == WM_GETMINMAXINFO)
                {
                    var dpi = GetDpiForWindow(hWnd);
                    var scale = dpi == 0 ? 1.0 : dpi / 96.0;

                    var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    mmi.ptMinTrackSize.X = (int)(MinWindowWidth * scale);
                    mmi.ptMinTrackSize.Y = (int)(MinWindowHeight * scale);
                    Marshal.StructureToPtr(mmi, lParam, fDeleteOld: false);
                    return IntPtr.Zero;
                }
            }
            catch { }

            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        // ─────────────────────────────────────────────
        //  Title bar inset
        //  ─────────────────────────────────────────────

        private void OnMainViewLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            UpdateTitleBarInset();
        }

        private void OnTitleBarReady(object? sender, FrameworkElement titleBar)
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(titleBar);

            DispatcherQueue.TryEnqueue(UpdateTitleBarInset);
        }

        private void UpdateTitleBarInset()
        {
            try
            {
                var inset = AppWindow.TitleBar.RightInset;

                // Fallback: some environments report 0 until the caption
                // buttons have been fully laid out.
                if (inset <= 0) inset = 138;

                MainViewContent.SetTitleBarRightInset(inset);
            }
            catch { }
        }

        private void OnThemeChanged(object? sender, EventArgs e)
            => DispatcherQueue.TryEnqueue(ApplyTheme);

        // ─────────────────────────────────────────────
        //  Icon
        //  ─────────────────────────────────────────────

        private void SetWindowIcon()
        {
            try
            {
                var iconPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory, "Assets", "DIHub.ico");

                if (System.IO.File.Exists(iconPath))
                {
                    AppWindow.SetIcon(iconPath);
                }
                else
                {
                    AppWindow.SetIcon("Assets/DIHub.ico");
                }
            }
            catch { }
        }

        // ─────────────────────────────────────────────
        //  Window state
        //  ─────────────────────────────────────────────

        private void RestoreWindowState()
        {
            try
            {
                var state = _settings.Current.WindowState ?? new WindowStateModel();

                var width = state.Width > 400 ? state.Width : 1400;
                var height = state.Height > 300 ? state.Height : 900;

                if (width < MinWindowWidth) width = MinWindowWidth;
                if (height < MinWindowHeight) height = MinWindowHeight;

                if (state.HasValidPosition && IsPositionOnAnyMonitor(state.X, state.Y, width, height))
                {
                    AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(state.X, state.Y, width, height));
                }
                else
                {
                    AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
                }

                if (state.IsMaximized && AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
            }
            catch { }
        }

        private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                var state = new WindowStateModel();

                if (AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    state.IsMaximized = presenter.State == OverlappedPresenterState.Maximized;
                }

                var size = AppWindow.Size;
                var pos = AppWindow.Position;

                state.Width = size.Width;
                state.Height = size.Height;

                if (!state.IsMaximized)
                {
                    state.X = pos.X;
                    state.Y = pos.Y;
                }
                else
                {
                    var prev = _settings.Current.WindowState;
                    state.X = prev?.X ?? -1;
                    state.Y = prev?.Y ?? -1;
                }

                _settings.Update(s => s.WindowState = state);
                _ = _settings.SaveAsync();
            }
            catch { }
        }

        private static bool IsPositionOnAnyMonitor(int x, int y, int width, int height)
        {
            try
            {
                var displays = DisplayArea.FindAll();
                if (displays is null || displays.Count == 0) return false;

                foreach (var display in displays)
                {
                    var bounds = display.WorkArea;
                    var overlapX = Math.Max(0, Math.Min(x + width, bounds.X + bounds.Width) - Math.Max(x, bounds.X));
                    var overlapY = Math.Max(0, Math.Min(y + height, bounds.Y + bounds.Height) - Math.Max(y, bounds.Y));

                    if (overlapX >= 100 && overlapY >= 100)
                        return true;
                }
            }
            catch { }

            return false;
        }

        // ─────────────────────────────────────────────
        //  Theme
        //  ─────────────────────────────────────────────

        private void ApplyTheme()
        {
            if (Content is not FrameworkElement root) return;

            try
            {
                var requested = _theme.CurrentTheme switch
                {
                    AppTheme.Light => ElementTheme.Light,
                    AppTheme.Dark => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };

                if (root.RequestedTheme != requested)
                    root.RequestedTheme = requested;

                var accentColor = MapAccent(_theme.CurrentAccent);

                if (Application.Current.Resources.TryGetValue("AppAccentBrush", out var existing)
                    && existing is SolidColorBrush brush)
                {
                    brush.Color = accentColor;
                }

                UpdateCaptionButtons(requested);
            }
            catch { }
        }

        private static Color MapAccent(AccentColor accent) => accent switch
        {
            AccentColor.Purple => Color.FromArgb(255, 0x8B, 0x5C, 0xF6),
            AccentColor.Blue => Color.FromArgb(255, 0x3B, 0x82, 0xF6),
            AccentColor.Cyan => Color.FromArgb(255, 0x06, 0xB6, 0xD4),
            AccentColor.Green => Color.FromArgb(255, 0x22, 0xC5, 0x5E),
            AccentColor.Orange => Color.FromArgb(255, 0xF9, 0x73, 0x16),
            _ => Color.FromArgb(255, 0x8B, 0x5C, 0xF6)
        };

        private void UpdateCaptionButtons(ElementTheme requested)
        {
            bool isLight = requested == ElementTheme.Light
                || (requested == ElementTheme.Default &&
                    Application.Current.RequestedTheme == ApplicationTheme.Light);

            var fg = isLight ? Color.FromArgb(255, 20, 20, 20) : Colors.White;
            var hoverBg = isLight ? Color.FromArgb(30, 0, 0, 0) : Color.FromArgb(255, 40, 40, 40);
            var pressedBg = isLight ? Color.FromArgb(60, 0, 0, 0) : Color.FromArgb(255, 60, 60, 60);

            var tb = AppWindow.TitleBar;
            tb.ButtonBackgroundColor = Colors.Transparent;
            tb.ButtonInactiveBackgroundColor = Colors.Transparent;
            tb.ButtonForegroundColor = fg;
            tb.ButtonInactiveForegroundColor = fg;
            tb.ButtonHoverBackgroundColor = hoverBg;
            tb.ButtonHoverForegroundColor = fg;
            tb.ButtonPressedBackgroundColor = pressedBg;
            tb.ButtonPressedForegroundColor = fg;
        }

        private void ConfigureTitleBar()
        {
            var tb = AppWindow.TitleBar;
            tb.ButtonBackgroundColor = Colors.Transparent;
            tb.ButtonInactiveBackgroundColor = Colors.Transparent;
            tb.ButtonForegroundColor = Colors.White;
            tb.ButtonHoverBackgroundColor = Color.FromArgb(255, 40, 40, 40);
            tb.ButtonHoverForegroundColor = Colors.White;
            tb.ButtonPressedBackgroundColor = Color.FromArgb(255, 60, 60, 60);
            tb.ButtonPressedForegroundColor = Colors.White;
        }

        private void ConfigureWindow()
        {
            AppWindow.Title = "DI Hub";

            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                SystemBackdrop = new MicaBackdrop
                {
                    Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                };
            }
        }
    }
}
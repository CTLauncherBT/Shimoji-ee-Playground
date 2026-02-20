using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ShimojiPlaygroundApp
{
    public class PlaygroundWindow : Window
    {
        private EditorSettings settings;
        private Window topOverlay, bottomOverlay, leftOverlay, rightOverlay;
        private DispatcherTimer followTimer, checkPositionTimer;
        private DateTime lastCheckTime;
        private Action returnToEditorAction;
        public bool isReturningToEditor = false;
        
        internal static class Win32
        {
            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_TOOLWINDOW = 0x00000080;

            public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

            public const int SWP_NOSIZE = 0x0001;
            public const int SWP_NOMOVE = 0x0002;
            public const int SWP_NOACTIVATE = 0x0010;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hwnd, int index);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetWindowPos(
                IntPtr hWnd,
                IntPtr hWndInsertAfter,
                int X,
                int Y,
                int cx,
                int cy,
                int uFlags
            );
        }

        public PlaygroundWindow(EditorSettings settings, Action returnToEditor)
        {
            this.settings = settings;
            this.returnToEditorAction = returnToEditor;

            InitializeWindow();
            InitializeOverlays();
            StartFollowTimer();
            StartPositionCheckTimer();
        }

        private void InitializeWindow()
        {
            this.Title = settings.WindowTitle;
            this.Width = settings.WindowWidth;
            this.Height = settings.WindowHeight;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.CanResize;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = settings.MainWindowTopMost;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Grid grid = new Grid();
            if (!string.IsNullOrEmpty(settings.BackgroundPath) && File.Exists(settings.BackgroundPath))
            {
                Image bg = new Image
                {
                    Source = new BitmapImage(new Uri(settings.BackgroundPath, UriKind.RelativeOrAbsolute)),
                    Stretch = Stretch.Fill
                };
                grid.Children.Add(bg);
            }

            this.Content = grid;
            this.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) this.DragMove(); };
            this.Opacity = 0;
            this.Loaded += (s, e) =>
            {
                var anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
                this.BeginAnimation(Window.OpacityProperty, anim);
            };
            this.KeyDown += PlaygroundWindow_KeyDown;
            lastCheckTime = DateTime.Now;
        }

        private void PlaygroundWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.X && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                isReturningToEditor = true;
                returnToEditorAction?.Invoke();
                this.Close();
            }
        }

        private void InitializeOverlays()
        {
            topOverlay = CreateOverlay(settings.TopOverlayPath, settings.TopHeight, true);
            bottomOverlay = CreateOverlay(settings.BottomOverlayPath, settings.BottomHeight, true);
            leftOverlay = CreateOverlay(settings.LeftOverlayPath, settings.LeftWidth, false);
            rightOverlay = CreateOverlay(settings.RightOverlayPath, settings.RightWidth, false);
            UpdateOverlayPositions();
        }

        private Window CreateOverlay(string path, double size, bool isVertical)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            Window overlay = new Window
            {
                Width = isVertical ? this.Width : size,
                Height = isVertical ? size : this.Height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = false
            };

            var hwnd = new System.Windows.Interop.WindowInteropHelper(overlay).EnsureHandle();
            int extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle | Win32.WS_EX_TOOLWINDOW);

            Image img = new Image { Source = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute)), Stretch = Stretch.Fill };
            overlay.Content = img;
            overlay.Show();

            return overlay;
        }

        private void StartFollowTimer()
        {
            followTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            followTimer.Tick += (s, e) => UpdateOverlayPositions();
            followTimer.Start();
        }

        private void UpdateOverlayPositions()
        {
            if (topOverlay != null) { topOverlay.Left = Left; topOverlay.Top = Top - topOverlay.Height; topOverlay.Width = Width; }
            if (bottomOverlay != null) { bottomOverlay.Left = Left; bottomOverlay.Top = Top + Height; bottomOverlay.Width = Width; }
            if (leftOverlay != null) { leftOverlay.Left = Left - leftOverlay.Width; leftOverlay.Top = Top; leftOverlay.Height = Height; }
            if (rightOverlay != null) { rightOverlay.Left = Left + Width; rightOverlay.Top = Top; rightOverlay.Height = Height; }
        }

        private void StartPositionCheckTimer()
        {
            checkPositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            checkPositionTimer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - lastCheckTime).TotalSeconds;
                if (elapsed >= 10) CheckWindowPosition();
            };
            checkPositionTimer.Start();
        }

        private void CheckWindowPosition()
        {
            var screen = SystemParameters.WorkArea;
            bool outOfScreen = Left + Width < 0 || Top + Height < 0 || Left > screen.Right || Top > screen.Bottom;
            if (!outOfScreen) return;
            double targetLeft = (screen.Width - Width) / 2;
            double targetTop = (screen.Height - Height) / 2;
            AnimateWindowPosition(targetLeft, targetTop);
            lastCheckTime = DateTime.Now;
        }

        private void AnimateWindowPosition(double targetLeft, double targetTop)
        {
            var animLeft = new DoubleAnimation(Left, targetLeft, TimeSpan.FromSeconds(2.5)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop };
            animLeft.Completed += (s, e) => Left = targetLeft;
            var animTop = new DoubleAnimation(Top, targetTop, TimeSpan.FromSeconds(2.5)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop };
            animTop.Completed += (s, e) => Top = targetTop;
            BeginAnimation(Window.LeftProperty, animLeft);
            BeginAnimation(Window.TopProperty, animTop);
        }

        protected override void OnClosed(EventArgs e)
        {
            followTimer?.Stop();
            checkPositionTimer?.Stop();
            topOverlay?.Close();
            bottomOverlay?.Close();
            leftOverlay?.Close();
            rightOverlay?.Close();
            base.OnClosed(e);
        }
    }
}
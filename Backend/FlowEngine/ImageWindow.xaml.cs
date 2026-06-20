using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Linq;

namespace FlowEngine
{
    public partial class ImageWindow : Window
    {
        public event EventHandler? WindowClosed;

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE = 0xF010;
        private const int SC_SIZE = 0xF000;
        private bool _isMoving = false;

        public ImageWindow()
        {
            InitializeComponent();
            ApplyCurrentTheme();
            Engine.ThemeManager.ThemeChanged += OnThemeChanged;
            this.Activated += ImageWindow_Activated;
        }

        private void ImageWindow_Activated(object? sender, EventArgs e)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                BringWindowToTop(helper.Handle);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private void OnThemeChanged()
        {
            Dispatcher.Invoke(ApplyCurrentTheme);
        }

        private void ApplyCurrentTheme()
        {
            this.Background = Engine.ThemeManager.PanelBgBrush;
            TitleBarBorder.Background = Engine.ThemeManager.DialogHeaderBgBrush;
            TitleBarBorder.BorderBrush = Engine.ThemeManager.BorderBrush;
            TitleTextBlock.Foreground = Engine.ThemeManager.DialogHeaderFgBrush;

            foreach (TabItem tabItem in ImageTabControl.Items)
            {
                bool isSelected = ImageTabControl.SelectedItem == tabItem;

                if (tabItem.Header is Border border)
                {
                    border.Background = isSelected ? Engine.ThemeManager.DialogHeaderBgBrush : Engine.ThemeManager.PanelBgBrush;
                    border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);

                    if (border.Child is StackPanel pnl)
                    {
                        foreach (var child in pnl.Children)
                        {
                            if (child is TextBlock tb)
                            {
                                tb.Foreground = isSelected ? Engine.ThemeManager.DialogHeaderFgBrush : Engine.ThemeManager.TextMutedBrush;
                            }
                            else if (child is Button btn)
                            {
                                btn.Foreground = isSelected ? Engine.ThemeManager.DialogHeaderFgBrush : Engine.ThemeManager.TextMutedBrush;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateImage(string title, BitmapSource bitmap)
        {
            foreach (TabItem item in ImageTabControl.Items)
            {
                if (item.Tag as string == title)
                {
                    if (item.Content is Border border && border.Child is Image image)
                    {
                        image.Source = bitmap;
                    }
                    break;
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
            source?.AddHook(HwndMessageHook);
        }

        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;
                if (command == SC_MOVE)
                {
                    _isMoving = true;
                }
                else if (command == SC_SIZE)
                {
                    _isMoving = false;
                }
            }
            else if (msg == WM_ENTERSIZEMOVE)
            {
                if (_isMoving)
                {
                    var mainWin = Application.Current.MainWindow as MainWindow;
                    mainWin?.ShowDockingGuide();
                }
            }
            else if (msg == WM_EXITSIZEMOVE)
            {
                if (_isMoving)
                {
                    CheckForDocking();
                    var mainWin = Application.Current.MainWindow as MainWindow;
                    mainWin?.HideDockingGuide();
                    mainWin?.HideDockPreview();
                }
                _isMoving = false;
            }
            return IntPtr.Zero;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            
            if (!_isMoving) return;

            var mainWin = Application.Current.MainWindow as MainWindow;
            if (mainWin != null && !mainWin.IsDockingGuideVisible)
            {
                mainWin.ShowDockingGuide();
            }

            CheckForDockingPreview();
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);



        public void AddImageTab(string title, BitmapSource imageSource)
        {
            var imgControl = new Image 
            { 
                Source = imageSource, 
                Stretch = System.Windows.Media.Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var tabHeader = new Border
            {
                Background = Engine.ThemeManager.TitleBarBgBrush,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 1, 0)
            };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var titleBlock = new TextBlock 
            { 
                Text = title, 
                Foreground = Engine.ThemeManager.TitleBarFgBrush, 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };

            var undockBtn = new Button 
            { 
                Content = "↗", 
                Width = 14, 
                Height = 14, 
                FontSize = 7, 
                Background = System.Windows.Media.Brushes.Transparent, 
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#a6adc8")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Undock to separate window",
                Style = (Style)FindResource("TitleBarButtonStyle")
            };
            undockBtn.Click += (s, e) => UndockTab(title);

            var closeBtn = new Button 
            { 
                Content = "✕", 
                Width = 14, 
                Height = 14, 
                FontSize = 7, 
                Background = System.Windows.Media.Brushes.Transparent, 
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#a6adc8")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Close tab",
                Style = (Style)FindResource("CloseButtonStyle")
            };
            closeBtn.Click += (s, e) => CloseTab(title);

            headerPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(undockBtn);
            headerPanel.Children.Add(closeBtn);
            tabHeader.Child = headerPanel;

            var tabItem = new TabItem 
            { 
                Header = tabHeader,
                Tag = title,
                Content = new Border 
                { 
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#11111b")),
                    Padding = new Thickness(0),
                    Child = imgControl 
                }
            };

            ImageTabControl.Items.Add(tabItem);
            ImageTabControl.SelectedItem = tabItem;

            // Sync window title bar
            this.Title = title;
            TitleTextBlock.Text = title;
        }

        public void AddGuiTab(string title, FrameworkElement guiContent)
        {
            var tabHeader = new Border
            {
                Background = Engine.ThemeManager.TitleBarBgBrush,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 1, 0)
            };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var titleBlock = new TextBlock 
            { 
                Text = Engine.GuiManager.GetDisplayName(title), 
                Foreground = Engine.ThemeManager.TitleBarFgBrush, 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };

            var undockBtn = new Button 
            { 
                Content = "↗", 
                Width = 14, 
                Height = 14, 
                FontSize = 7, 
                Background = System.Windows.Media.Brushes.Transparent, 
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#a6adc8")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Undock to separate window",
                Style = (Style)FindResource("TitleBarButtonStyle")
            };
            undockBtn.Click += (s, e) => UndockGuiTab(title);

            var closeBtn = new Button 
            { 
                Content = "✕", 
                Width = 14, 
                Height = 14, 
                FontSize = 7, 
                Background = System.Windows.Media.Brushes.Transparent, 
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#a6adc8")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Close tab",
                Style = (Style)FindResource("CloseButtonStyle")
            };
            closeBtn.Click += (s, e) => CloseGuiTab(title);

            headerPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(undockBtn);
            headerPanel.Children.Add(closeBtn);
            tabHeader.Child = headerPanel;

            var tabItem = new TabItem 
            { 
                Header = tabHeader,
                Tag = title,
                Content = new Border 
                { 
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#11111b")),
                    Padding = new Thickness(0),
                    Child = guiContent 
                }
            };

            ImageTabControl.Items.Add(tabItem);
            ImageTabControl.SelectedItem = tabItem;

            // Sync window title bar
            this.Title = Engine.GuiManager.GetDisplayName(title);
            TitleTextBlock.Text = Engine.GuiManager.GetDisplayName(title);
        }

        private void UndockGuiTab(string title)
        {
            TabItem? targetTab = null;
            foreach (TabItem item in ImageTabControl.Items)
            {
                if (item.Tag as string == title)
                {
                    targetTab = item;
                    break;
                }
            }

            if (targetTab != null)
            {
                FrameworkElement? content = null;
                if (targetTab.Content is Border border)
                {
                    content = border.Child as FrameworkElement;
                    border.Child = null;
                }

                ImageTabControl.Items.Remove(targetTab);

                if (content != null)
                {
                    Engine.GuiManager.ShowFloatingGuiDialog(title, content);
                }

                if (ImageTabControl.Items.Count == 0)
                {
                    this.Close();
                }
            }
        }

        private void CloseGuiTab(string title)
        {
            TabItem? targetTab = null;
            foreach (TabItem item in ImageTabControl.Items)
            {
                if (item.Tag as string == title)
                {
                    targetTab = item;
                    break;
                }
            }

            if (targetTab != null)
            {
                if (targetTab.Content is Border border)
                {
                    border.Child = null;
                }
                ImageTabControl.Items.Remove(targetTab);
                Engine.GuiManager.HideGuiDialog(title);

                if (ImageTabControl.Items.Count == 0)
                {
                    this.Close();
                }
            }
        }

        private void UndockTab(string title)
        {
            TabItem? targetTab = null;
            foreach (TabItem item in ImageTabControl.Items)
            {
                if (item.Tag as string == title)
                {
                    targetTab = item;
                    break;
                }
            }

            if (targetTab != null)
            {
                BitmapSource? source = null;
                if (targetTab.Content is Border border && border.Child is Image image)
                {
                    source = image.Source as BitmapSource;
                }

                ImageTabControl.Items.Remove(targetTab);

                if (source != null)
                {
                    Engine.OpenCvLuaApi.ShowFloatingWindow(title, source);
                }

                if (ImageTabControl.Items.Count == 0)
                {
                    this.Close();
                }
            }
        }

        private void CloseTab(string title)
        {
            TabItem? targetTab = null;
            foreach (TabItem item in ImageTabControl.Items)
            {
                if (item.Tag as string == title)
                {
                    targetTab = item;
                    break;
                }
            }

            if (targetTab != null)
            {
                ImageTabControl.Items.Remove(targetTab);
                Engine.OpenCvLuaApi.RemoveImage(title);

                if (ImageTabControl.Items.Count == 0)
                {
                    this.Close();
                }
            }
        }

        private void ImageTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageTabControl.SelectedItem is TabItem selectedTab)
            {
                string title = selectedTab.Tag as string ?? "Image View";
                this.Title = title;
                this.TitleTextBlock.Text = title;
            }
            ApplyCurrentTheme();
        }

        public void ShowTabPreview()
        {
            TabPreviewOverlay.Visibility = Visibility.Visible;
        }

        public void HideTabPreview()
        {
            TabPreviewOverlay.Visibility = Visibility.Collapsed;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Engine.ThemeManager.ThemeChanged -= OnThemeChanged;
            this.Activated -= ImageWindow_Activated;
            
            // Clean up all tab registrations in OpenCV API
            foreach (TabItem item in ImageTabControl.Items)
            {
                if (item.Tag is string title)
                {
                    Engine.OpenCvLuaApi.RemoveImage(title);
                }
            }

            WindowClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}

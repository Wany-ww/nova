using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlowEngine
{
    public partial class MainWindow : Window
    {

                /// <summary>
        /// Sets the size dimension of a docking panel based on its direction.
        /// </summary>
        /// <param name="direction">The dock direction ("Left", "Right", "Top", "Bottom")</param>
        /// <param name="size">The new size in pixels</param>
        private void SetDockDimension(string direction, double size)
        {
            switch (direction)
            {
                case "Left":
                    LeftDockCol.Width = new GridLength(size);
                    break;
                case "Right":
                    RightDockCol.Width = new GridLength(size);
                    break;
                case "Top":
                    TopDockCol.Height = new GridLength(size);
                    break;
                case "Bottom":
                    BottomDockCol.Height = new GridLength(size);
                    break;
            }
        }

                /// <summary>
        /// Displays the visual docking preview highlight at a target side of the window.
        /// </summary>
        /// <param name="direction">The direction to show preview for</param>
        public void ShowDockPreview(string direction)
        {
            Dispatcher.Invoke(() =>
            {
                if (_overlayDockPreview == null || _dockingOverlayWindow == null) return;

                var contentArea = LeftDockPanelBorder.Parent as Grid;
                if (contentArea == null) return;

                double rootWidth = contentArea.ActualWidth;
                double rootHeight = contentArea.ActualHeight;

                double x = 0, y = 0, w = 0, h = 0;

                switch (direction)
                {
                    case "Left":
                        x = 0;
                        y = 0;
                        w = 300;
                        h = rootHeight;
                        break;
                    case "Right":
                        x = rootWidth - 300;
                        y = 0;
                        w = 300;
                        h = rootHeight;
                        break;
                    case "Top":
                        x = 0;
                        y = 0;
                        w = rootWidth;
                        h = 200;
                        break;
                    case "Bottom":
                        x = 0;
                        y = rootHeight - 200;
                        w = rootWidth;
                        h = 200;
                        break;
                }

                Canvas.SetLeft(_overlayDockPreview, x);
                Canvas.SetTop(_overlayDockPreview, y);
                _overlayDockPreview.Width = w;
                _overlayDockPreview.Height = h;
                _overlayDockPreview.Visibility = Visibility.Visible;
            });
        }

                /// <summary>
        /// Hides the active visual docking preview highlight.
        /// </summary>
        public void HideDockPreview()
        {
            Dispatcher.Invoke(() =>
            {
                if (_overlayDockPreview != null)
                {
                    _overlayDockPreview.Visibility = Visibility.Collapsed;
                }
            });
        }

                /// <summary>
        /// Shows the center overlay window containing the root docking guide icons.
        /// </summary>
        public void ShowDockingGuide()
        {
            Dispatcher.Invoke(() =>
            {
                if (_dockingOverlayWindow != null) return;

                // Position overlay window on top of the main window's Row 1 area
                var contentArea = LeftDockPanelBorder.Parent as Grid;
                if (contentArea == null) return;

                Point contentScreenPos = contentArea.PointToScreen(new Point(0, 0));
                double contentWidth = contentArea.ActualWidth;
                double contentHeight = contentArea.ActualHeight;

                _dockingOverlayWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    ShowInTaskbar = false,
                    IsHitTestVisible = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = contentScreenPos.X,
                    Top = contentScreenPos.Y,
                    Width = contentWidth,
                    Height = contentHeight,
                    Owner = this
                };

                // Use a Canvas so we can position guides and previews using coordinates relative to contentArea
                var rootCanvas = new Canvas();

                _overlayDockPreview = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(80, 137, 180, 250)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    Visibility = Visibility.Collapsed
                };
                rootCanvas.Children.Add(_overlayDockPreview);

                // Initialize Root Docking Guide (160x160)
                var guideBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(240, 30, 30, 46)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(49, 50, 68)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Width = 160,
                    Height = 160,
                    Opacity = 0.95
                };

                var guideGrid = new Grid();
                for (int i = 0; i < 3; i++)
                {
                    guideGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    guideGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                _guideTop = CreateGuideTarget("▲", "#89b4fa");
                Grid.SetRow(_guideTop, 0); Grid.SetColumn(_guideTop, 1);
                guideGrid.Children.Add(_guideTop);

                _guideLeft = CreateGuideTarget("◀", "#89b4fa");
                Grid.SetRow(_guideLeft, 1); Grid.SetColumn(_guideLeft, 0);
                guideGrid.Children.Add(_guideLeft);

                _guideCenter = CreateGuideTarget("■", "#a6e3a1");
                Grid.SetRow(_guideCenter, 1); Grid.SetColumn(_guideCenter, 1);
                guideGrid.Children.Add(_guideCenter);

                _guideRight = CreateGuideTarget("▶", "#89b4fa");
                Grid.SetRow(_guideRight, 1); Grid.SetColumn(_guideRight, 2);
                guideGrid.Children.Add(_guideRight);

                _guideBottom = CreateGuideTarget("▼", "#89b4fa");
                Grid.SetRow(_guideBottom, 2); Grid.SetColumn(_guideBottom, 1);
                guideGrid.Children.Add(_guideBottom);

                guideBorder.Child = guideGrid;

                _overlayDockingGuide = new Grid { Width = 160, Height = 160 };
                _overlayDockingGuide.Children.Add(guideBorder);
                rootCanvas.Children.Add(_overlayDockingGuide);

                // Position root guide in the center of the webView area relative to contentArea
                var webViewArea = webView.Parent as FrameworkElement;
                if (webViewArea != null)
                {
                    Point webViewPos = webViewArea.TranslatePoint(new Point(0, 0), contentArea);
                    Canvas.SetLeft(_overlayDockingGuide, webViewPos.X + (webViewArea.ActualWidth - 160) / 2);
                    Canvas.SetTop(_overlayDockingGuide, webViewPos.Y + (webViewArea.ActualHeight - 160) / 2);
                }

                // Initialize Panel Docking Guide (120x120)
                var panelGuideBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(240, 30, 30, 46)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(49, 50, 68)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Width = 120,
                    Height = 120,
                    Opacity = 0.95
                };

                var panelGuideGrid = new Grid();
                for (int i = 0; i < 3; i++)
                {
                    panelGuideGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    panelGuideGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                _panelGuideTop = CreateGuideTarget("▲", "#89b4fa");
                Grid.SetRow(_panelGuideTop, 0); Grid.SetColumn(_panelGuideTop, 1);
                panelGuideGrid.Children.Add(_panelGuideTop);

                _panelGuideLeft = CreateGuideTarget("◀", "#89b4fa");
                Grid.SetRow(_panelGuideLeft, 1); Grid.SetColumn(_panelGuideLeft, 0);
                panelGuideGrid.Children.Add(_panelGuideLeft);

                _panelGuideCenter = CreateGuideTarget("■", "#a6e3a1");
                Grid.SetRow(_panelGuideCenter, 1); Grid.SetColumn(_panelGuideCenter, 1);
                panelGuideGrid.Children.Add(_panelGuideCenter);

                _panelGuideRight = CreateGuideTarget("▶", "#89b4fa");
                Grid.SetRow(_panelGuideRight, 1); Grid.SetColumn(_panelGuideRight, 2);
                panelGuideGrid.Children.Add(_panelGuideRight);

                _panelGuideBottom = CreateGuideTarget("▼", "#89b4fa");
                Grid.SetRow(_panelGuideBottom, 2); Grid.SetColumn(_panelGuideBottom, 1);
                panelGuideGrid.Children.Add(_panelGuideBottom);

                panelGuideBorder.Child = panelGuideGrid;

                _panelDockingGuide = new Grid { Width = 120, Height = 120, Visibility = Visibility.Collapsed };
                _panelDockingGuide.Children.Add(panelGuideBorder);
                rootCanvas.Children.Add(_panelDockingGuide);

                _dockingOverlayWindow.Content = rootCanvas;
                _dockingOverlayWindow.Show();

                HighlightDockingGuide(null);
                HighlightPanelDockingGuide(null);
            });
        }

                /// <summary>
        /// Helper to create a single stylized guide target block (arrow/center square).
        /// </summary>
        /// <param name="text">The symbol text (▲, ◀, ■, ▶, ▼)</param>
        /// <param name="fgColorHex">The hex color for the text foreground</param>
        /// <returns>A styled Border element containing the target text</returns>
        private Border CreateGuideTarget(string text, string fgColorHex)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(49, 50, 68)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 71, 90)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(4)
            };
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgColorHex)),
                FontSize = text == "■" ? 18 : 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            border.Child = textBlock;
            return border;
        }

                /// <summary>
        /// Hides the central docking guide overlay window and resets all targets.
        /// </summary>
        public void HideDockingGuide()
        {
            Dispatcher.Invoke(() =>
            {
                if (_dockingOverlayWindow != null)
                {
                    _dockingOverlayWindow.Close();
                    _dockingOverlayWindow = null;
                    _overlayDockingGuide = null;
                    _panelDockingGuide = null;
                    _overlayDockPreview = null;
                    _guideTop = null;
                    _guideLeft = null;
                    _guideCenter = null;
                    _guideRight = null;
                    _guideBottom = null;
                    _panelGuideTop = null;
                    _panelGuideLeft = null;
                    _panelGuideCenter = null;
                    _panelGuideRight = null;
                    _panelGuideBottom = null;
                    _hoveredTabControl = null;
                }
            });
        }

                /// <summary>
        /// Highlights the target icon of the root docking guide depending on hover direction.
        /// </summary>
        /// <param name="direction">The direction to highlight, or null to clear highlight</param>
        public void HighlightDockingGuide(string? direction)
        {
            Dispatcher.Invoke(() =>
            {
                if (_guideTop == null || _guideLeft == null || _guideRight == null || _guideBottom == null || _guideCenter == null) return;

                var inactiveBrush = new SolidColorBrush(Color.FromRgb(49, 50, 68));
                var activeBrush = new SolidColorBrush(Color.FromRgb(137, 180, 250));
                var centerInactive = new SolidColorBrush(Color.FromRgb(69, 71, 90));
                var centerActive = new SolidColorBrush(Color.FromRgb(166, 227, 161));

                _guideTop.Background = direction == "Top" ? activeBrush : inactiveBrush;
                _guideLeft.Background = direction == "Left" ? activeBrush : inactiveBrush;
                _guideRight.Background = direction == "Right" ? activeBrush : inactiveBrush;
                _guideBottom.Background = direction == "Bottom" ? activeBrush : inactiveBrush;
                _guideCenter.Background = direction == "Center" ? centerActive : centerInactive;
            });
        }

                /// <summary>
        /// Highlights the target icon of the panel-specific docking guide.
        /// </summary>
        /// <param name="direction">The direction to highlight, or null to clear highlight</param>
        public void HighlightPanelDockingGuide(string? direction)
        {
            Dispatcher.Invoke(() =>
            {
                if (_panelGuideTop == null || _panelGuideLeft == null || _panelGuideRight == null || _panelGuideBottom == null || _panelGuideCenter == null) return;

                var inactiveBrush = new SolidColorBrush(Color.FromRgb(49, 50, 68));
                var activeBrush = new SolidColorBrush(Color.FromRgb(137, 180, 250));
                var centerInactive = new SolidColorBrush(Color.FromRgb(69, 71, 90));
                var centerActive = new SolidColorBrush(Color.FromRgb(166, 227, 161));

                _panelGuideTop.Background = direction == "Top" ? activeBrush : inactiveBrush;
                _panelGuideLeft.Background = direction == "Left" ? activeBrush : inactiveBrush;
                _panelGuideRight.Background = direction == "Right" ? activeBrush : inactiveBrush;
                _panelGuideBottom.Background = direction == "Bottom" ? activeBrush : inactiveBrush;
                _panelGuideCenter.Background = direction == "Center" ? centerActive : centerInactive;
            });
        }

                /// <summary>
        /// Positions the local panel docking guide centered over a specific hovered tab control.
        /// </summary>
        /// <param name="tabControl">The target TabControl to position the guide over</param>
        public void PositionPanelDockingGuide(TabControl tabControl)
        {
            Dispatcher.Invoke(() =>
            {
                if (_panelDockingGuide == null || _dockingOverlayWindow == null) return;

                var contentArea = LeftDockPanelBorder.Parent as Grid;
                if (contentArea == null) return;

                try
                {
                    Point tabControlPos = tabControl.TranslatePoint(new Point(0, 0), contentArea);
                    double left = tabControlPos.X + (tabControl.ActualWidth - 120) / 2;
                    double top = tabControlPos.Y + (tabControl.ActualHeight - 120) / 2;

                    Canvas.SetLeft(_panelDockingGuide, left);
                    Canvas.SetTop(_panelDockingGuide, top);
                    _panelDockingGuide.Visibility = Visibility.Visible;
                    _hoveredTabControl = tabControl;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to position panel guide: {ex.Message}");
                    // Suppress: throw/logCallback/MessageBox not needed here
                }
            });
        }

                /// <summary>
        /// Hides the panel-specific docking guide and resets the hovered control state.
        /// </summary>
        public void HidePanelDockingGuide()
        {
            Dispatcher.Invoke(() =>
            {
                if (_panelDockingGuide != null)
                {
                    _panelDockingGuide.Visibility = Visibility.Collapsed;
                }
                _hoveredTabControl = null;
            });
        }

                /// <summary>
        /// Computes mouse cursor coordinates relative to the panel docking guide.
        /// </summary>
        /// <param name="screenPoint">The screen coordinates of the cursor</param>
        /// <returns>The localized cursor coordinates inside the panel guide</returns>
        public Point GetOverlayPanelGuidePoint(Point screenPoint)
        {
            return Dispatcher.Invoke(() =>
            {
                if (_panelDockingGuide == null) return new Point(-999, -999);
                try
                {
                    return _panelDockingGuide.PointFromScreen(screenPoint);
                }
                catch
                {
                    return new Point(-999, -999);
                }
            });
        }

        /// <summary>
        /// Highlights a preview partition (left half, right half, etc.) within a specific tab control.
        /// </summary>
        /// <param name="tabControl">The TabControl being hovered over</param>
        /// <param name="direction">The partition direction to highlight</param>
        public void ShowPanelDockPreview(TabControl tabControl, string direction)
        {
            Dispatcher.Invoke(() =>
            {
                if (_overlayDockPreview == null || _dockingOverlayWindow == null) return;

                var contentArea = LeftDockPanelBorder.Parent as Grid;
                if (contentArea == null) return;

                try
                {
                    Point tabPos = tabControl.TranslatePoint(new Point(0, 0), contentArea);
                    double w = tabControl.ActualWidth;
                    double h = tabControl.ActualHeight;

                    double x = tabPos.X;
                    double y = tabPos.Y;

                    switch (direction)
                    {
                        case "Center":
                            // full area
                            break;
                        case "Left":
                            w /= 2;
                            break;
                        case "Right":
                            x += w / 2;
                            w /= 2;
                            break;
                        case "Top":
                            h /= 2;
                            break;
                        case "Bottom":
                            y += h / 2;
                            h /= 2;
                            break;
                    }

                    Canvas.SetLeft(_overlayDockPreview, x);
                    Canvas.SetTop(_overlayDockPreview, y);
                    _overlayDockPreview.Width = w;
                    _overlayDockPreview.Height = h;
                    _overlayDockPreview.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to show panel preview: {ex.Message}");
                    // Suppress: throw/logCallback/MessageBox not needed here
                }
            });
        }
    }
}


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
        public TabControl? HoveredTabControl => _hoveredTabControl;


        public List<TabControl> ActiveTabControls
        {
            get
            {
                var list = new List<TabControl>();
                FindTabControls(LeftDockPanelBorder, list);
                FindTabControls(RightDockPanelBorder, list);
                FindTabControls(TopDockPanelBorder, list);
                FindTabControls(BottomDockPanelBorder, list);
                return list;
            }
        }

                /// <summary>
        /// Recursively traverses the visual tree to find all active TabControl instances.
        /// </summary>
        /// <param name="parent">The root UI element to start scanning from</param>
        /// <param name="result">The accumulated list of found TabControls</param>
        private void FindTabControls(DependencyObject parent, List<TabControl> result)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TabControl tabControl)
                {
                    result.Add(tabControl);
                }
                else
                {
                    FindTabControls(child, result);
                }
            }
        }

                /// <summary>
        /// Recursively traverses the visual tree to find all active GridSplitter instances.
        /// </summary>
        /// <param name="parent">The root UI element to start scanning from</param>
        /// <param name="result">The accumulated list of found GridSplitters</param>
        private void FindGridSplitters(DependencyObject parent, List<GridSplitter> result)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is GridSplitter splitter)
                {
                    result.Add(splitter);
                }
                else
                {
                    FindGridSplitters(child, result);
                }
            }
        }

        public bool IsDockingGuideVisible => Dispatcher.Invoke(() => _dockingOverlayWindow != null);

                /// <summary>
        /// Computes mouse cursor coordinates relative to the root central docking guide.
        /// </summary>
        /// <param name="screenPoint">The screen coordinates of the cursor</param>
        /// <returns>The localized cursor coordinates inside the central guide</returns>
        public Point GetOverlayGuidePoint(Point screenPoint)
        {
            return Dispatcher.Invoke(() =>
            {
                if (_overlayDockingGuide == null) return new Point(-999, -999);
                try
                {
                    return _overlayDockingGuide.PointFromScreen(screenPoint);
                }
                catch
                {
                    return new Point(-999, -999);
                }
            });
        }

        public TabControl GetTabControl(string direction)
        {
            return direction switch
            {
                "Left" => LeftDockTabControl,
                "Right" => RightDockTabControl,
                "Top" => TopDockTabControl,
                "Bottom" => BottomDockTabControl,
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        private Border GetPanelBorder(string direction)
        {
            return direction switch
            {
                "Left" => LeftDockPanelBorder,
                "Right" => RightDockPanelBorder,
                "Top" => TopDockPanelBorder,
                "Bottom" => BottomDockPanelBorder,
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        private GridSplitter GetSplitter(string direction)
        {
            return direction switch
            {
                "Left" => LeftDockSplitter,
                "Right" => RightDockSplitter,
                "Top" => TopDockSplitter,
                "Bottom" => BottomDockSplitter,
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        private void UpdateDockedTabsTheme()
        {
            Dispatcher.Invoke(() =>
            {
                string[] directions = { "Left", "Right", "Top", "Bottom" };
                foreach (var dir in directions)
                {
                    var panel = GetPanelBorder(dir);
                    panel.Background = Engine.ThemeManager.PanelBgBrush;
                    panel.BorderBrush = Engine.ThemeManager.BorderBrush;

                    var tabControls = new List<TabControl>();
                    FindTabControls(panel, tabControls);

                    foreach (var tabControl in tabControls)
                    {
                        foreach (TabItem tabItem in tabControl.Items)
                        {
                            bool isSelected = tabControl.SelectedItem == tabItem;

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

                    var splitters = new List<GridSplitter>();
                    FindGridSplitters(panel, splitters);
                    foreach (var splitter in splitters)
                    {
                        splitter.Background = Engine.ThemeManager.BorderBrush;
                    }
                }
            });
        }

        private TabControl GetOrCreateActiveTabControl(string direction)
        {
            var panel = GetPanelBorder(direction);
            var list = new List<TabControl>();
            FindTabControls(panel, list);
            if (list.Count > 0)
            {
                return list[0];
            }
            
            var rootTabControl = GetTabControl(direction);
            panel.Child = rootTabControl;
            return rootTabControl;
        }

        public TabControl? FindTabControlByTitle(string title)
        {
            return Dispatcher.Invoke(() =>
            {
                string[] directions = { "Left", "Right", "Top", "Bottom" };
                foreach (var dir in directions)
                {
                    var panel = GetPanelBorder(dir);
                    var tabControls = new List<TabControl>();
                    FindTabControls(panel, tabControls);
                    foreach (var tabControl in tabControls)
                    {
                        foreach (TabItem item in tabControl.Items)
                        {
                            if (item.Tag as string == title)
                                return tabControl;
                        }
                    }
                }
                return null;
            });
        }

        private Border? FindParentBorder(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is Border border && (border == LeftDockPanelBorder || border == RightDockPanelBorder || border == TopDockPanelBorder || border == BottomDockPanelBorder))
            {
                return border;
            }
            return FindParentBorder(parent);
        }

        private string? GetDockPanelDirection(Border border)
        {
            if (border == LeftDockPanelBorder) return "Left";
            if (border == RightDockPanelBorder) return "Right";
            if (border == TopDockPanelBorder) return "Top";
            if (border == BottomDockPanelBorder) return "Bottom";
            return null;
        }

                /// <summary>
        /// Splits a TabControl container horizontally or vertically, introducing a new panel and a splitter.
        /// </summary>
        /// <param name="targetTabControl">The existing TabControl being split</param>
        /// <param name="newTabControl">The new TabControl containing the dropped tab</param>
        /// <param name="direction">The split direction ("Left", "Right", "Top", "Bottom")</param>
        public void SplitDockContainer(TabControl targetTabControl, TabControl newTabControl, string direction)
        {
            var parent = targetTabControl.Parent as FrameworkElement;
            if (parent == null) return;

            int row = 0;
            int col = 0;
            int rowSpan = 1;
            int colSpan = 1;

            if (parent is Grid parentGrid)
            {
                row = Grid.GetRow(targetTabControl);
                col = Grid.GetColumn(targetTabControl);
                rowSpan = Grid.GetRowSpan(targetTabControl);
                colSpan = Grid.GetColumnSpan(targetTabControl);
                parentGrid.Children.Remove(targetTabControl);
            }
            else if (parent is Border parentBorder)
            {
                parentBorder.Child = null;
            }

            var splitGrid = new Grid();
            var splitter = new GridSplitter
            {
                Background = Engine.ThemeManager.BorderBrush,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            if (direction == "Left" || direction == "Right")
            {
                splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
                splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                splitter.Width = 3;
                splitter.Cursor = Cursors.SizeWE;
                Grid.SetColumn(splitter, 1);
                splitGrid.Children.Add(splitter);

                if (direction == "Left")
                {
                    Grid.SetColumn(newTabControl, 0);
                    Grid.SetColumn(targetTabControl, 2);
                }
                else
                {
                    Grid.SetColumn(targetTabControl, 0);
                    Grid.SetColumn(newTabControl, 2);
                }
            }
            else
            {
                splitGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                splitGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
                splitGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                splitter.Height = 3;
                splitter.Cursor = Cursors.SizeNS;
                Grid.SetRow(splitter, 1);
                splitGrid.Children.Add(splitter);

                if (direction == "Top")
                {
                    Grid.SetRow(newTabControl, 0);
                    Grid.SetRow(targetTabControl, 2);
                }
                else
                {
                    Grid.SetRow(targetTabControl, 0);
                    Grid.SetRow(newTabControl, 2);
                }
            }

            splitGrid.Children.Add(targetTabControl);
            splitGrid.Children.Add(newTabControl);

            if (parent is Grid pg)
            {
                Grid.SetRow(splitGrid, row);
                Grid.SetColumn(splitGrid, col);
                Grid.SetRowSpan(splitGrid, rowSpan);
                Grid.SetColumnSpan(splitGrid, colSpan);
                pg.Children.Add(splitGrid);
            }
            else if (parent is Border pb)
            {
                pb.Child = splitGrid;
            }

            UpdateDockedTabsTheme();
        }

                /// <summary>
        /// Cleans up empty TabControls recursively, promoting siblings and collapsing grids to keep the tree clean.
        /// </summary>
        /// <param name="tabControl">The empty TabControl that needs removal</param>
        private void CleanUpEmptyTabControl(TabControl tabControl)
        {
            if (tabControl.Items.Count > 0) return;

            var parent = tabControl.Parent as FrameworkElement;
            if (parent == null) return;

            if (parent is Border rootBorder)
            {
                string? direction = GetDockPanelDirection(rootBorder);
                if (direction != null)
                {
                    UpdateDockPanelVisibility(direction);
                }
                return;
            }

            if (parent is Grid splitGrid)
            {
                UIElement? sibling = null;
                foreach (UIElement child in splitGrid.Children)
                {
                    if (child != tabControl && !(child is GridSplitter))
                    {
                        sibling = child;
                        break;
                    }
                }

                var grandParent = splitGrid.Parent as FrameworkElement;
                if (grandParent == null) return;

                splitGrid.Children.Clear();

                if (sibling != null)
                {
                    if (grandParent is Border grandBorder)
                    {
                        grandBorder.Child = sibling;
                    }
                    else if (grandParent is Grid grandGrid)
                    {
                        int row = Grid.GetRow(splitGrid);
                        int col = Grid.GetColumn(splitGrid);
                        int rowSpan = Grid.GetRowSpan(splitGrid);
                        int colSpan = Grid.GetColumnSpan(splitGrid);

                        grandGrid.Children.Remove(splitGrid);

                        Grid.SetRow(sibling, row);
                        Grid.SetColumn(sibling, col);
                        Grid.SetRowSpan(sibling, rowSpan);
                        Grid.SetColumnSpan(sibling, colSpan);

                        grandGrid.Children.Add(sibling);
                    }

                    var rootBorderOfSplit = FindParentBorder(sibling);
                    if (rootBorderOfSplit != null)
                    {
                        string? direction = GetDockPanelDirection(rootBorderOfSplit);
                        if (direction != null)
                        {
                            UpdateDockPanelVisibility(direction);
                        }
                    }
                }
            }
        }

        public void AddTabToTabControl(TabControl tabControl, string title, BitmapSource imageSource)
        {
            Dispatcher.Invoke(() =>
            {
                var oldTabControl = FindTabControlByTitle(title);
                if (oldTabControl != null)
                {
                    TabItem? oldTab = null;
                    foreach (TabItem item in oldTabControl.Items)
                    {
                        if (item.Tag as string == title)
                        {
                            oldTab = item;
                            break;
                        }
                    }
                    if (oldTab != null)
                    {
                        oldTabControl.Items.Remove(oldTab);
                        CleanUpEmptyTabControl(oldTabControl);
                    }
                }

                var imgControl = new Image 
                { 
                    Source = imageSource, 
                    Stretch = Stretch.Uniform,
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

                var rootBorder = FindParentBorder(tabControl);
                string direction = rootBorder != null ? (GetDockPanelDirection(rootBorder) ?? "Right") : "Right";

                var undockBtn = new Button 
                { 
                    Content = "↗", 
                    Width = 14, 
                    Height = 14, 
                    FontSize = 7, 
                    Background = Brushes.Transparent, 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6adc8")),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Undock to floating window",
                    Style = (Style)FindResource("TitleBarButtonStyle")
                };
                undockBtn.Click += (s, e) => UndockTab(title, direction);

                var closeBtn = new Button 
                { 
                    Content = "✕", 
                    Width = 14, 
                    Height = 14, 
                    FontSize = 7, 
                    Background = Brushes.Transparent, 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6adc8")),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Close image",
                    Style = (Style)FindResource("CloseButtonStyle")
                };
                closeBtn.Click += (s, e) => CloseTab(title, direction);

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
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11111b")),
                        Padding = new Thickness(0),
                        Child = imgControl 
                    }
                };

                tabControl.Items.Add(tabItem);
                tabControl.SelectedItem = tabItem;

                UpdateDockedTabsTheme();
            });
        }

        public void DockImageWindow(string title, BitmapSource imageSource, string direction)
        {
            Dispatcher.Invoke(() =>
            {
                var panel = GetPanelBorder(direction);
                var splitter = GetSplitter(direction);
                var tabControl = GetOrCreateActiveTabControl(direction);

                panel.Visibility = Visibility.Visible;
                splitter.Visibility = Visibility.Visible;

                double currentSize = 0;
                switch (direction)
                {
                    case "Left": currentSize = LeftDockCol.Width.Value; break;
                    case "Right": currentSize = RightDockCol.Width.Value; break;
                    case "Top": currentSize = TopDockCol.Height.Value; break;
                    case "Bottom": currentSize = BottomDockCol.Height.Value; break;
                }
                if (currentSize <= 0)
                {
                    SetDockDimension(direction, direction == "Left" || direction == "Right" ? 300 : 200);
                }

                AddTabToTabControl(tabControl, title, imageSource);
            });
        }

        private void UndockTab(string title, string direction)
        {
            Dispatcher.Invoke(() =>
            {
                var tabControl = FindTabControlByTitle(title);
                if (tabControl == null) return;
                
                TabItem? targetTab = null;
                foreach (TabItem item in tabControl.Items)
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

                    tabControl.Items.Remove(targetTab);
                    CleanUpEmptyTabControl(tabControl);

                    if (source != null)
                    {
                        Engine.OpenCvLuaApi.ShowFloatingWindow(title, source);
                    }
                }
            });
        }

        private void CloseTab(string title, string direction)
        {
            Dispatcher.Invoke(() =>
            {
                var tabControl = FindTabControlByTitle(title);
                if (tabControl == null) return;

                TabItem? targetTab = null;
                foreach (TabItem item in tabControl.Items)
                {
                    if (item.Tag as string == title)
                    {
                        targetTab = item;
                        break;
                    }
                }

                if (targetTab != null)
                {
                    tabControl.Items.Remove(targetTab);
                    CleanUpEmptyTabControl(tabControl);
                    Engine.OpenCvLuaApi.RemoveImage(title);
                }
            });
        }

        private void UpdateDockPanelVisibility(string direction)
        {
            var panel = GetPanelBorder(direction);
            var splitter = GetSplitter(direction);

            var tabControls = new List<TabControl>();
            FindTabControls(panel, tabControls);

            int totalItems = tabControls.Sum(tc => tc.Items.Count);

            if (totalItems == 0)
            {
                panel.Visibility = Visibility.Collapsed;
                splitter.Visibility = Visibility.Collapsed;
                SetDockDimension(direction, 0);

                var rootTabControl = GetTabControl(direction);
                rootTabControl.Items.Clear();
                panel.Child = rootTabControl;
            }
        }

        public bool IsImageDocked(string title)
        {
            return Dispatcher.Invoke(() =>
            {
                string[] directions = { "Left", "Right", "Top", "Bottom" };
                foreach (var dir in directions)
                {
                    var panel = GetPanelBorder(dir);
                    var tabControls = new List<TabControl>();
                    FindTabControls(panel, tabControls);
                    foreach (var tabControl in tabControls)
                    {
                        foreach (TabItem item in tabControl.Items)
                        {
                            if (item.Tag as string == title)
                                return true;
                        }
                    }
                }
                return false;
            });
        }

        public string? GetDockedDirection(string title)
        {
            return Dispatcher.Invoke(() =>
            {
                string[] directions = { "Left", "Right", "Top", "Bottom" };
                foreach (var dir in directions)
                {
                    var panel = GetPanelBorder(dir);
                    var tabControls = new List<TabControl>();
                    FindTabControls(panel, tabControls);
                    foreach (var tabControl in tabControls)
                    {
                        foreach (TabItem item in tabControl.Items)
                        {
                            if (item.Tag as string == title)
                                return dir;
                        }
                    }
                }
                return null;
            });
        }


    }
}

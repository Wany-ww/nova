using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace FlowEngine
{
    public partial class ImageWindow : Window
    {
                /// <summary>
        /// Checks if the cursor is hovering over the central root docking guide and handles preview highlights.
        /// </summary>
        /// <param name="mainWin">The MainWindow instance reference</param>
        /// <param name="screenPt">The mouse screen point coordinates</param>
        /// <returns>True if cursor is hovering over a valid central guide target, otherwise False</returns>
        private bool CheckRootDockingGuide(MainWindow mainWin, POINT screenPt)
        {
            Point guidePt = mainWin.GetOverlayGuidePoint(new Point(screenPt.X, screenPt.Y));

            if (guidePt.X >= 0 && guidePt.X <= 160 && guidePt.Y >= 0 && guidePt.Y <= 160)
            {
                int col = (int)(guidePt.X / 53.33);
                int row = (int)(guidePt.Y / 53.33);

                string? direction = null;
                if (row == 0 && col == 1) direction = "Top";
                else if (row == 1 && col == 0) direction = "Left";
                else if (row == 1 && col == 1) direction = "Center";
                else if (row == 1 && col == 2) direction = "Right";
                else if (row == 2 && col == 1) direction = "Bottom";

                if (direction != null)
                {
                    mainWin.HighlightDockingGuide(direction);
                    mainWin.HidePanelDockingGuide();

                    string previewDir = direction;
                    if (previewDir == "Center")
                    {
                        previewDir = "Right";
                        string[] directions = { "Right", "Left", "Bottom", "Top" };
                        foreach (var dir in directions)
                        {
                            var tabControl = mainWin.GetTabControl(dir);
                            if (tabControl.Items.Count > 0)
                            {
                                previewDir = dir;
                                break;
                            }
                        }
                    }
                    mainWin.ShowDockPreview(previewDir);

                    // Hide any tab preview on other floating windows
                    foreach (var win in Engine.OpenCvLuaApi.GetActiveImageWindows())
                    {
                        win.HideTabPreview();
                    }
                    return true;
                }
            }
            return false;
        }

                /// <summary>
        /// Checks if the cursor is hovering over any docking panel area and activates local panel docking guides.
        /// </summary>
        /// <param name="mainWin">The MainWindow instance reference</param>
        /// <param name="screenPt">The mouse screen point coordinates</param>
        /// <returns>True if cursor is hovering over an active panel, otherwise False</returns>
        private bool CheckPanelDockingGuide(MainWindow mainWin, POINT screenPt)
        {
            TabControl? hoverTabCtrl = null;
            foreach (var tabCtrl in mainWin.ActiveTabControls)
            {
                if (tabCtrl.IsVisible)
                {
                    Point tabCtrlPos = tabCtrl.PointToScreen(new Point(0, 0));
                    double w = tabCtrl.ActualWidth;
                    double h = tabCtrl.ActualHeight;

                    if (screenPt.X >= tabCtrlPos.X && screenPt.X <= tabCtrlPos.X + w &&
                        screenPt.Y >= tabCtrlPos.Y && screenPt.Y <= tabCtrlPos.Y + h)
                    {
                        hoverTabCtrl = tabCtrl;
                        break;
                    }
                }
            }

            if (hoverTabCtrl != null)
            {
                mainWin.PositionPanelDockingGuide(hoverTabCtrl);

                Point panelGuidePt = mainWin.GetOverlayPanelGuidePoint(new Point(screenPt.X, screenPt.Y));
                if (panelGuidePt.X >= 0 && panelGuidePt.X <= 120 && panelGuidePt.Y >= 0 && panelGuidePt.Y <= 120)
                {
                    int col = (int)(panelGuidePt.X / 40);
                    int row = (int)(panelGuidePt.Y / 40);

                    string? direction = null;
                    if (row == 0 && col == 1) direction = "Top";
                    else if (row == 1 && col == 0) direction = "Left";
                    else if (row == 1 && col == 1) direction = "Center";
                    else if (row == 1 && col == 2) direction = "Right";
                    else if (row == 2 && col == 1) direction = "Bottom";

                    if (direction != null)
                    {
                        mainWin.HighlightPanelDockingGuide(direction);
                        mainWin.ShowPanelDockPreview(hoverTabCtrl, direction);

                        // Hide any tab preview on other floating windows
                        foreach (var win in Engine.OpenCvLuaApi.GetActiveImageWindows())
                        {
                            win.HideTabPreview();
                        }
                        return true;
                    }
                }

                // Inside hovered TabControl but not hovering guide target
                mainWin.HighlightPanelDockingGuide(null);
                mainWin.HideDockPreview();
                return true;
            }

            mainWin.HidePanelDockingGuide();
            return false;
        }

                /// <summary>
        /// Checks if the cursor is hovering over another floating ImageWindow to show tab merging previews.
        /// </summary>
        /// <param name="screenPt">The mouse screen point coordinates</param>
        private void CheckFloatingWindowMergePreview(POINT screenPt)
        {
            ImageWindow? hoverWin = null;
            foreach (var win in Engine.OpenCvLuaApi.GetActiveImageWindows())
            {
                if (win == this || !win.IsVisible) continue;

                double left = win.Left;
                double top = win.Top;
                double right = left + win.ActualWidth;
                double bottom = top + win.ActualHeight;

                if (screenPt.X >= left && screenPt.X <= right && screenPt.Y >= top && screenPt.Y <= bottom)
                {
                    hoverWin = win;
                    break;
                }
            }

            foreach (var win in Engine.OpenCvLuaApi.GetActiveImageWindows())
            {
                if (win == hoverWin)
                {
                    win.ShowTabPreview();
                }
                else
                {
                    win.HideTabPreview();
                }
            }
        }

                /// <summary>
        /// Triggered periodically during a drag operation to hit-test overlays and display docking guides/previews.
        /// </summary>
        private void CheckForDockingPreview()
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            if (mainWin == null) return;

            POINT screenPt;
            if (!GetCursorPos(out screenPt)) return;

            try
            {
                if (CheckRootDockingGuide(mainWin, screenPt)) return;
                mainWin.HighlightDockingGuide(null);

                if (CheckPanelDockingGuide(mainWin, screenPt)) return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Docking preview failed: {ex.Message}");
                // Suppress: throw/logCallback/MessageBox not needed for docking preview
            }

            mainWin.HideDockPreview();
            CheckFloatingWindowMergePreview(screenPt);
        }

                /// <summary>
        /// Checks and performs docking of tabs into a sub-panel partition upon drag drop release.
        /// </summary>
        /// <param name="mainWin">The MainWindow instance reference</param>
        /// <param name="screenPt">The mouse screen point coordinates</param>
        /// <returns>True if tab docking was successfully executed, otherwise False</returns>
        private bool CheckPanelDocking(MainWindow mainWin, POINT screenPt)
        {
            var hoverTabCtrl = mainWin.HoveredTabControl;
            if (hoverTabCtrl == null) return false;

            Point panelGuidePt = mainWin.GetOverlayPanelGuidePoint(new Point(screenPt.X, screenPt.Y));
            if (panelGuidePt.X >= 0 && panelGuidePt.X <= 120 && panelGuidePt.Y >= 0 && panelGuidePt.Y <= 120)
            {
                int col = (int)(panelGuidePt.X / 40);
                int row = (int)(panelGuidePt.Y / 40);

                string? direction = null;
                if (row == 0 && col == 1) direction = "Top";
                else if (row == 1 && col == 0) direction = "Left";
                else if (row == 1 && col == 1) direction = "Center";
                else if (row == 1 && col == 2) direction = "Right";
                else if (row == 2 && col == 1) direction = "Bottom";

                if (direction != null)
                {
                    var tabsToDock = ImageTabControl.Items.Cast<TabItem>().ToList();
                    ImageTabControl.Items.Clear();

                    if (direction == "Center")
                    {
                        foreach (var tabItem in tabsToDock)
                        {
                            string title = tabItem.Tag as string ?? "";
                            BitmapSource? imgSource = null;
                            FrameworkElement? guiContent = null;
                            if (tabItem.Content is Border border)
                            {
                                if (border.Child is Image img)
                                {
                                    imgSource = img.Source as BitmapSource;
                                }
                                else
                                {
                                    guiContent = border.Child as FrameworkElement;
                                    border.Child = null;
                                }
                            }
                            if (!string.IsNullOrEmpty(title))
                            {
                                if (imgSource != null)
                                {
                                    mainWin.AddTabToTabControl(hoverTabCtrl, title, imgSource);
                                }
                                else if (guiContent != null)
                                {
                                    mainWin.AddGuiTabToTabControl(hoverTabCtrl, title, guiContent);
                                }
                            }
                        }
                    }
                    else
                    {
                        var newTabControl = new TabControl
                        {
                            Background = Brushes.Transparent,
                            BorderThickness = new Thickness(0),
                            Margin = new Thickness(0),
                            Padding = new Thickness(0)
                        };
                        newTabControl.SelectionChanged += mainWin.DockTabControl_SelectionChanged;

                        foreach (var tabItem in tabsToDock)
                        {
                            string title = tabItem.Tag as string ?? "";
                            BitmapSource? imgSource = null;
                            FrameworkElement? guiContent = null;
                            if (tabItem.Content is Border border)
                            {
                                if (border.Child is Image img)
                                {
                                    imgSource = img.Source as BitmapSource;
                                }
                                else
                                {
                                    guiContent = border.Child as FrameworkElement;
                                    border.Child = null;
                                }
                            }
                            if (!string.IsNullOrEmpty(title))
                            {
                                if (imgSource != null)
                                {
                                    mainWin.AddTabToTabControl(newTabControl, title, imgSource);
                                }
                                else if (guiContent != null)
                                {
                                    mainWin.AddGuiTabToTabControl(newTabControl, title, guiContent);
                                }
                            }
                        }

                        mainWin.SplitDockContainer(hoverTabCtrl, newTabControl, direction);
                    }

                    this.Close();
                    return true;
                }
            }
            return false;
        }

                /// <summary>
        /// Checks and performs docking of tabs into the central/root panel zones upon drag drop release.
        /// </summary>
        /// <param name="mainWin">The MainWindow instance reference</param>
        /// <param name="screenPt">The mouse screen point coordinates</param>
        /// <returns>True if central docking was successfully executed, otherwise False</returns>
        private bool CheckRootDocking(MainWindow mainWin, POINT screenPt)
        {
            Point guidePt = mainWin.GetOverlayGuidePoint(new Point(screenPt.X, screenPt.Y));

            if (guidePt.X >= 0 && guidePt.X <= 160 && guidePt.Y >= 0 && guidePt.Y <= 160)
            {
                int col = (int)(guidePt.X / 53.33);
                int row = (int)(guidePt.Y / 53.33);

                string? direction = null;
                if (row == 0 && col == 1) direction = "Top";
                else if (row == 1 && col == 0) direction = "Left";
                else if (row == 1 && col == 1) direction = "Center";
                else if (row == 1 && col == 2) direction = "Right";
                else if (row == 2 && col == 1) direction = "Bottom";

                if (direction != null)
                {
                    string targetDir = direction;
                    if (targetDir == "Center")
                    {
                        targetDir = "Right";
                        string[] directions = { "Right", "Left", "Bottom", "Top" };
                        foreach (var dir in directions)
                        {
                            var tabControl = mainWin.GetTabControl(dir);
                            if (tabControl.Items.Count > 0)
                            {
                                targetDir = dir;
                                break;
                            }
                        }
                    }

                    var tabsToDock = ImageTabControl.Items.Cast<TabItem>().ToList();
                    ImageTabControl.Items.Clear();

                    foreach (var tabItem in tabsToDock)
                    {
                        string title = tabItem.Tag as string ?? "";
                        BitmapSource? imgSource = null;
                        FrameworkElement? guiContent = null;
                        if (tabItem.Content is Border border)
                        {
                            if (border.Child is Image img)
                            {
                                imgSource = img.Source as BitmapSource;
                            }
                            else
                            {
                                guiContent = border.Child as FrameworkElement;
                                border.Child = null;
                            }
                        }
                        if (!string.IsNullOrEmpty(title))
                        {
                            if (imgSource != null)
                            {
                                mainWin.DockImageWindow(title, imgSource, targetDir);
                            }
                            else if (guiContent != null)
                            {
                                mainWin.DockGuiWindow(title, guiContent, targetDir);
                            }
                        }
                    }

                    this.Close();
                    return true;
                }
            }
            return false;
        }

                /// <summary>
        /// Checks and merges this floating window with another floating image window upon drag drop release.
        /// </summary>
        /// <param name="screenPt">The mouse screen point coordinates</param>
        /// <returns>True if merge was completed, otherwise False</returns>
        private bool CheckFloatingWindowMerge(POINT screenPt)
        {
            ImageWindow? targetWin = null;
            foreach (var win in Engine.OpenCvLuaApi.GetActiveImageWindows())
            {
                if (win == this || !win.IsVisible) continue;

                double left = win.Left;
                double top = win.Top;
                double right = left + win.ActualWidth;
                double bottom = top + win.ActualHeight;

                if (screenPt.X >= left && screenPt.X <= right && screenPt.Y >= top && screenPt.Y <= bottom)
                {
                    targetWin = win;
                    break;
                }
            }

            if (targetWin != null)
            {
                var tabsToMove = ImageTabControl.Items.Cast<TabItem>().ToList();
                ImageTabControl.Items.Clear();

                foreach (var tabItem in tabsToMove)
                {
                    string title = tabItem.Tag as string ?? "";
                    BitmapSource? imgSource = null;
                    FrameworkElement? guiContent = null;
                    if (tabItem.Content is Border border)
                    {
                        if (border.Child is Image img)
                        {
                            imgSource = img.Source as BitmapSource;
                        }
                        else
                        {
                            guiContent = border.Child as FrameworkElement;
                            border.Child = null;
                        }
                    }
                    if (!string.IsNullOrEmpty(title))
                    {
                        if (imgSource != null)
                        {
                            targetWin.AddImageTab(title, imgSource);
                            Engine.OpenCvLuaApi.RegisterImageWindow(title, targetWin);
                        }
                        else if (guiContent != null)
                        {
                            targetWin.AddGuiTab(title, guiContent);
                            Engine.GuiManager.RegisterGuiWindow(title, targetWin);
                        }
                    }
                }

                this.Close();
                return true;
            }
            return false;
        }

                /// <summary>
        /// Triggered on drag drop release to finalize the docking or merge operation based on current cursor position.
        /// </summary>
        private void CheckForDocking()
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            if (mainWin == null) return;

            POINT screenPt;
            if (!GetCursorPos(out screenPt)) return;

            // Clear any active tab previews on all image windows
            foreach (var win in Engine.OpenCvLuaApi.GetActiveImageWindows())
            {
                win.HideTabPreview();
            }

            try
            {
                if (CheckPanelDocking(mainWin, screenPt)) return;
                if (CheckRootDocking(mainWin, screenPt)) return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Docking check failed: {ex.Message}");
                // Suppress: throw/logCallback/MessageBox not needed for docking checks
            }

            CheckFloatingWindowMerge(screenPt);
        }
    }
}

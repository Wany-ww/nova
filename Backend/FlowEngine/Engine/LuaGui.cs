using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MoonSharp.Interpreter;

namespace FlowEngine.Engine
{
    public class GuiWidget
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public FrameworkElement Element { get; set; } = null!;
        public Canvas? ContainerCanvas { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
        public DynValue? OnClick { get; set; }
        public DynValue? OnHover { get; set; }
        public DynValue? OnChanged { get; set; }
        public DynValue? OnDoubleClick { get; set; }
        public bool IsHorizontal { get; set; } = false;

        // Data series properties for plotline widgets
        public List<double> Plot2DData { get; set; } = new List<double>();
        public List<List<double>> Plot3DData { get; set; } = new List<List<double>>();
        public string Legend { get; set; } = string.Empty;
        public Brush? CustomColor { get; set; }
    }

    public class GuiDialog
    {
        public string Name { get; }
        public ScrollViewer ScrollViewer { get; }
        public Canvas RootCanvas { get; }
        public Dictionary<string, GuiWidget> Widgets { get; } = new Dictionary<string, GuiWidget>();
        public Brush? CustomBackground { get; set; }

        public GuiDialog(string name)
        {
            Name = name;
            RootCanvas = new Canvas
            {
                Background = ThemeManager.PanelBgBrush,
                Width = 400,
                Height = 300
            };

            ScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = RootCanvas
            };

            ThemeManager.ThemeChanged += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (CustomBackground != null)
                    {
                        RootCanvas.Background = CustomBackground;
                        ScrollViewer.Background = CustomBackground;
                    }
                    else
                    {
                        RootCanvas.Background = ThemeManager.PanelBgBrush;
                        ScrollViewer.Background = Brushes.Transparent;
                    }
                });
            };
        }
    }

    public static class GuiManager
    {
        private static readonly Dictionary<string, GuiDialog> _dialogs = new Dictionary<string, GuiDialog>();
        private static readonly Dictionary<string, GuiWidget> _widgets = new Dictionary<string, GuiWidget>();
        private static readonly Dictionary<string, ImageWindow> _activeGuiWindows = new Dictionary<string, ImageWindow>();
        private static readonly object _lock = new object();

        private static readonly Brush[] SeriesColors = new Brush[]
        {
            ThemeManager.AccentBrush,
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f5c2e7")), // Pink
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6e3a1")), // Green
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fab387")), // Peach
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b4befe")), // Lavender
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8")), // Red
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9e2af")), // Yellow
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94e2d5"))  // Teal
        };

        public static Brush? TryGetDialogBackground(string name)
        {
            lock (_lock)
            {
                if (_dialogs.TryGetValue(name, out var dialog))
                {
                    return dialog.CustomBackground;
                }
            }
            return null;
        }

        private static List<GuiWidget> GetChildPlotLines(string parentName)
        {
            var children = new List<GuiWidget>();
            lock (_lock)
            {
                foreach (var widget in _widgets.Values)
                {
                    if (widget.ParentName == parentName && widget.Type == "plotline")
                    {
                        children.Add(widget);
                    }
                }
            }
            return children;
        }

        public static Script? CurrentScript { get; private set; }

        public static void RegisterGuiApis(Script script)
        {
            lock (_lock)
            {
                CurrentScript = script;
            }

            var guiTable = new Table(script);
            var dialogTable = new Table(script);
            var widgetTable = new Table(script);
            var configTable = new Table(script);

            dialogTable["create"] = (Action<string>)(name => CreateDialog(name));
            dialogTable["show"] = (Action<string, bool>)((name, visible) => ShowDialog(name, visible));

            widgetTable["create"] = (Action<string, string, string>)((name, type, parent) => CreateWidget(name, type, parent));

            configTable["set"] = (Action<string, string, string, DynValue>)((name, type, key, value) => SetConfig(name, type, key, value));

            guiTable["dialog"] = dialogTable;
            guiTable["widget"] = widgetTable;
            guiTable["config"] = configTable;

            script.Globals["gui"] = guiTable;
        }

        public static void CreateDialog(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            lock (_lock)
            {
                if (_dialogs.ContainsKey(name)) return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (_dialogs.ContainsKey(name)) return;
                    var dialog = new GuiDialog(name);
                    _dialogs[name] = dialog;
                }
            });
        }

        public static void ShowDialog(string name, bool visible)
        {
            if (string.IsNullOrEmpty(name)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                GuiDialog? dialog;
                lock (_lock)
                {
                    if (!_dialogs.TryGetValue(name, out dialog)) return;
                }

                var mainWin = Application.Current.MainWindow as MainWindow;
                if (mainWin == null) return;

                if (visible)
                {
                    var tabControl = mainWin.FindTabControlByTitle(name);
                    if (tabControl != null)
                    {
                        foreach (TabItem item in tabControl.Items)
                        {
                            if (item.Tag as string == name)
                            {
                                tabControl.SelectedItem = item;
                                break;
                            }
                        }
                    }
                    else if (_activeGuiWindows.TryGetValue(name, out var win) && win.IsLoaded)
                    {
                        win.Focus();
                    }
                    else
                    {
                        ShowFloatingGuiDialog(name, dialog.ScrollViewer);
                    }
                }
                else
                {
                    var tabControl = mainWin.FindTabControlByTitle(name);
                    if (tabControl != null)
                    {
                        TabItem? targetTab = null;
                        foreach (TabItem item in tabControl.Items)
                        {
                            if (item.Tag as string == name)
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
                            tabControl.Items.Remove(targetTab);
                            mainWin.CleanUpEmptyTabControl(tabControl);
                        }
                    }

                    if (_activeGuiWindows.TryGetValue(name, out var win))
                    {
                        win.Close();
                    }
                }
            });
        }

        public static void ShowFloatingGuiDialog(string name, FrameworkElement content)
        {
            if (_activeGuiWindows.TryGetValue(name, out var window) && window.IsLoaded)
            {
                window.Focus();
            }
            else
            {
                var win = new ImageWindow();
                win.AddGuiTab(name, content);
                _activeGuiWindows[name] = win;
                win.WindowClosed += (s, e) =>
                {
                    lock (_lock)
                    {
                        _activeGuiWindows.Remove(name);
                    }
                };
                win.Show();
            }
        }

        public static void RegisterGuiWindow(string name, ImageWindow win)
        {
            lock (_lock)
            {
                _activeGuiWindows[name] = win;
            }
        }

        public static void HideGuiDialog(string name)
        {
            lock (_lock)
            {
                _activeGuiWindows.Remove(name);
            }
        }

        public static void CreateWidget(string name, string type, string parent)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(parent)) return;

            lock (_lock)
            {
                if (_widgets.ContainsKey(name)) return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (_widgets.ContainsKey(name)) return;

                    Canvas? parentCanvas = null;
                    GuiDialog? parentDialog = null;
                    GuiWidget? parentWidget = null;

                    if (_dialogs.TryGetValue(parent, out parentDialog))
                    {
                        parentCanvas = parentDialog.RootCanvas;
                    }
                    else if (_widgets.TryGetValue(parent, out parentWidget) && (parentWidget.Type == "panel" || parentWidget.Type == "plot2d" || parentWidget.Type == "plot3d"))
                    {
                        if (parentWidget.Type == "panel")
                        {
                            parentCanvas = parentWidget.ContainerCanvas;
                        }
                        else
                        {
                            parentCanvas = parentWidget.Element as Canvas;
                        }
                    }

                    if (parentCanvas == null) return;

                    FrameworkElement? element = null;
                    Canvas? containerCanvas = null;

                    string displayName = GetDisplayName(name);

                    switch (type)
                    {
                        case "panel":
                            containerCanvas = new Canvas { Background = Brushes.Transparent, Width = 200, Height = 150 };
                            element = new Border
                            {
                                Width = 200,
                                Height = 150,
                                Background = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255)),
                                BorderBrush = ThemeManager.BorderBrush,
                                BorderThickness = new Thickness(1),
                                Child = containerCanvas
                            };
                            break;

                        case "button":
                            element = new Button { Content = displayName, Width = 80, Height = 22 };
                            break;

                        case "label":
                            element = new TextBlock { Text = displayName };
                            break;

                        case "slider":
                            element = new Slider { Minimum = 0, Maximum = 100, Value = 0, Width = 120, Height = 22 };
                            break;

                        case "checkbox":
                            element = new CheckBox { Content = displayName };
                            break;

                        case "dropdown":
                            element = new ComboBox { Width = 100, Height = 22 };
                            break;

                        case "textinput":
                            element = new TextBox { Width = 100, Height = 22 };
                            break;

                        case "image":
                            element = new Image { Stretch = Stretch.Uniform, Width = 150, Height = 150 };
                            break;

                        case "plot2d":
                            element = new Canvas { Width = 200, Height = 150 };
                            break;

                        case "plot3d":
                            element = new Canvas { Width = 200, Height = 150 };
                            break;

                        case "progress":
                            element = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Width = 120, Height = 15 };
                            break;

                        case "colorpicker":
                            element = new Border
                            {
                                Width = 40,
                                Height = 22,
                                Background = Brushes.Red,
                                BorderThickness = new Thickness(1),
                                BorderBrush = ThemeManager.BorderBrush
                            };
                            break;

                        case "plotline":
                            element = new FrameworkElement();
                            break;

                        default:
                            return;
                    }

                    ApplyNovaStyle(element);

                    parentCanvas.Children.Add(element);
                    Canvas.SetLeft(element, 0);
                    Canvas.SetTop(element, 0);

                    var widget = new GuiWidget
                    {
                        Name = name,
                        Type = type,
                        ParentName = parent,
                        Element = element,
                        ContainerCanvas = containerCanvas,
                        Position = new Point(0, 0),
                        Size = new Size(double.IsNaN(element.Width) ? double.NaN : element.Width, double.IsNaN(element.Height) ? double.NaN : element.Height)
                    };

                    HookWidgetEvents(widget);

                    _widgets[name] = widget;
                    if (parentDialog != null)
                    {
                        parentDialog.Widgets[name] = widget;
                    }

                    if (parentWidget != null && parentWidget.Type == "panel" && parentWidget.IsHorizontal)
                    {
                        UpdatePanelLayout(parentWidget);
                    }
                }
            });
        }

        public static void SetConfig(string name, string type, string key, DynValue value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(key)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (type == "dialog")
                {
                    GuiDialog? dialog;
                    lock (_lock)
                    {
                        if (!_dialogs.TryGetValue(name, out dialog)) return;
                    }

                    switch (key.ToLower())
                    {
                        case "background_color":
                        case "backgroud_color":
                            var bgBrush = ParseColor(value);
                            if (bgBrush != null)
                            {
                                dialog.CustomBackground = bgBrush;
                                dialog.RootCanvas.Background = bgBrush;
                                dialog.ScrollViewer.Background = bgBrush;
                                if (dialog.ScrollViewer.Parent is Border parentBorder)
                                {
                                    parentBorder.Background = bgBrush;
                                }
                            }
                            break;
                        case "size":
                            var size = ParseSize(value);
                            if (size != null)
                            {
                                dialog.RootCanvas.Width = size.Value.Width;
                                dialog.RootCanvas.Height = size.Value.Height;
                            }
                            break;
                    }
                    return;
                }

                GuiWidget? widget;
                lock (_lock)
                {
                    if (!_widgets.TryGetValue(name, out widget) || widget.Type != type) return;
                }

                switch (key.ToLower())
                {
                    case "size":
                        var size = ParseSize(value);
                        if (size != null)
                        {
                            widget.Element.Width = size.Value.Width;
                            widget.Element.Height = size.Value.Height;
                            widget.Size = size.Value;
                            if (widget.ContainerCanvas != null)
                            {
                                widget.ContainerCanvas.Width = size.Value.Width;
                                widget.ContainerCanvas.Height = size.Value.Height;
                            }
                            UpdateParentCanvasSize(widget);

                            if (!string.IsNullOrEmpty(widget.ParentName))
                            {
                                GuiWidget? parentWidget;
                                lock (_lock)
                                {
                                    _widgets.TryGetValue(widget.ParentName, out parentWidget);
                                }
                                if (parentWidget != null && parentWidget.Type == "panel" && parentWidget.IsHorizontal)
                                {
                                    UpdatePanelLayout(parentWidget);
                                }
                            }
                        }
                        break;

                    case "horizontal":
                        if (widget.Type == "panel")
                        {
                            widget.IsHorizontal = value.CastToBool();
                            UpdatePanelLayout(widget);
                        }
                        break;

                    case "legend":
                        if (widget.Type == "plot2d" && widget.Element is Canvas plotCanvas2d)
                        {
                            if (!(plotCanvas2d.Tag is Plot2DState state2d))
                            {
                                state2d = new Plot2DState();
                                plotCanvas2d.Tag = state2d;
                            }
                            state2d.Legend = value.CastToString() ?? string.Empty;
                            RenderPlot2D(plotCanvas2d);
                        }
                        else if (widget.Type == "plot3d" && widget.Element is Canvas plotCanvas3d)
                        {
                            if (!(plotCanvas3d.Tag is Plot3DState state3d))
                            {
                                state3d = new Plot3DState();
                                plotCanvas3d.Tag = state3d;
                            }
                            state3d.Legend = value.CastToString() ?? string.Empty;
                            RenderPlot3D(plotCanvas3d);
                        }
                        else if (widget.Type == "plotline")
                        {
                            widget.Legend = value.CastToString() ?? string.Empty;
                            GuiWidget? parentWidget = null;
                            lock (_lock)
                            {
                                _widgets.TryGetValue(widget.ParentName, out parentWidget);
                            }
                            if (parentWidget != null && parentWidget.Element is Canvas parentCanvas)
                            {
                                if (parentWidget.Type == "plot2d") RenderPlot2D(parentCanvas);
                                else if (parentWidget.Type == "plot3d") RenderPlot3D(parentCanvas);
                            }
                        }
                        break;

                    case "pos":
                        var pos = ParsePos(value);
                        if (pos != null)
                        {
                            Canvas.SetLeft(widget.Element, pos.Value.X);
                            Canvas.SetTop(widget.Element, pos.Value.Y);
                            widget.Position = pos.Value;
                            UpdateParentCanvasSize(widget);
                        }
                        break;

                    case "foreground_color":
                        var fgBrush = ParseColor(value);
                        if (fgBrush != null)
                        {
                            if (widget.Element is Control control) control.Foreground = fgBrush;
                            else if (widget.Element is TextBlock tb) tb.Foreground = fgBrush;
                            else if (widget.Type == "plotline")
                            {
                                widget.CustomColor = fgBrush;
                                GuiWidget? parentWidget = null;
                                lock (_lock)
                                {
                                    _widgets.TryGetValue(widget.ParentName, out parentWidget);
                                }
                                if (parentWidget != null && parentWidget.Element is Canvas parentCanvas)
                                {
                                    if (parentWidget.Type == "plot2d") RenderPlot2D(parentCanvas);
                                    else if (parentWidget.Type == "plot3d") RenderPlot3D(parentCanvas);
                                }
                            }
                        }
                        break;

                    case "background_color":
                    case "backgroud_color":
                        var bgBrush = ParseColor(value);
                        if (bgBrush != null)
                        {
                            if (widget.Element is Panel panelControl) panelControl.Background = bgBrush;
                            else if (widget.Element is Border borderControl) borderControl.Background = bgBrush;
                            else if (widget.Element is Control control) control.Background = bgBrush;
                        }
                        break;

                    case "onclick":
                        widget.OnClick = value;
                        break;

                    case "onhover":
                        widget.OnHover = value;
                        break;

                    case "onchanged":
                        widget.OnChanged = value;
                        break;

                    case "ondoubleclick":
                        widget.OnDoubleClick = value;
                        break;

                    case "label":
                        if (value.Type == DataType.String)
                        {
                            string displayLabel = GetDisplayName(value.String);
                            if (widget.Element is Button btn) btn.Content = displayLabel;
                            else if (widget.Element is TextBlock textBlock) textBlock.Text = displayLabel;
                            else if (widget.Element is CheckBox cb) cb.Content = displayLabel;
                        }
                        break;

                    case "range":
                        if (widget.Element is Slider slider && value.Type == DataType.Table)
                        {
                            var tbl = value.Table;
                            if (tbl.Length >= 2)
                            {
                                slider.Minimum = tbl.Get(1).Number;
                                slider.Maximum = tbl.Get(2).Number;
                            }
                        }
                        break;

                    case "step":
                        if (widget.Element is Slider s)
                        {
                            s.TickFrequency = value.Number;
                            s.IsSnapToTickEnabled = true;
                        }
                        break;

                    case "menus":
                        if (widget.Element is ComboBox combo && value.Type == DataType.Table)
                        {
                            combo.Items.Clear();
                            var tbl = value.Table;
                            for (int i = 1; i <= tbl.Length; i++)
                            {
                                combo.Items.Add(tbl.Get(i).String);
                            }
                        }
                        break;

                    case "data":
                        if (widget.Element is Slider sl)
                        {
                            sl.Value = value.Number;
                        }
                        else if (widget.Element is CheckBox checkBox)
                        {
                            checkBox.IsChecked = value.Boolean;
                        }
                        else if (widget.Element is ComboBox com)
                        {
                            int idx = (int)value.Number - 1;
                            if (idx >= 0 && idx < com.Items.Count) com.SelectedIndex = idx;
                        }
                        else if (widget.Element is TextBox textBox)
                        {
                            textBox.Text = value.String;
                        }
                        else if (widget.Element is Image imgControl)
                        {
                            if (value.UserData?.Object is MatWrapper mat && mat.Mat != null)
                            {
                                imgControl.Source = OpenCvLuaApi.ConvertMatToBitmapSource(mat.Mat);
                            }
                            else if (value.Type == DataType.Table)
                            {
                                var tbl = value.Table;
                                byte[] bytes = new byte[tbl.Length];
                                for (int i = 0; i < tbl.Length; i++)
                                {
                                    bytes[i] = (byte)tbl.Get(i + 1).Number;
                                }
                                var bitmap = new BitmapImage();
                                using (var stream = new MemoryStream(bytes))
                                {
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = stream;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                }
                                imgControl.Source = bitmap;
                            }
                        }
                        else if (widget.Element is Canvas plotCanvas && widget.Type == "plot2d" && value.Type == DataType.Table)
                        {
                            var tbl = value.Table;
                            List<double> pts = new List<double>();
                            for (int i = 1; i <= tbl.Length; i++)
                            {
                                pts.Add(tbl.Get(i).Number);
                            }
                            if (!(plotCanvas.Tag is Plot2DState state))
                            {
                                state = new Plot2DState();
                                plotCanvas.Tag = state;
                            }
                            state.Data = pts;
                            RenderPlot2D(plotCanvas);
                        }
                        else if (widget.Element is Canvas plot3dCanvas && widget.Type == "plot3d" && value.Type == DataType.Table)
                        {
                            var tbl = value.Table;
                            List<List<double>> grid = new List<List<double>>();
                            for (int i = 1; i <= tbl.Length; i++)
                            {
                                var rowVal = tbl.Get(i);
                                if (rowVal.Type == DataType.Table)
                                {
                                    var rowList = new List<double>();
                                    for (int j = 1; j <= rowVal.Table.Length; j++)
                                    {
                                        rowList.Add(rowVal.Table.Get(j).Number);
                                    }
                                    grid.Add(rowList);
                                }
                            }
                            if (!(plot3dCanvas.Tag is Plot3DState state))
                            {
                                state = new Plot3DState();
                                plot3dCanvas.Tag = state;
                            }
                            state.GridData = grid;
                            RenderPlot3D(plot3dCanvas);
                        }
                        else if (widget.Type == "plotline" && value.Type == DataType.Table)
                        {
                            GuiWidget? parentWidget = null;
                            lock (_lock)
                            {
                                _widgets.TryGetValue(widget.ParentName, out parentWidget);
                            }
                            if (parentWidget != null)
                            {
                                if (parentWidget.Type == "plot2d")
                                {
                                    var tbl = value.Table;
                                    List<double> pts = new List<double>();
                                    for (int i = 1; i <= tbl.Length; i++)
                                    {
                                        pts.Add(tbl.Get(i).Number);
                                    }
                                    widget.Plot2DData = pts;
                                    if (parentWidget.Element is Canvas parentCanvas)
                                    {
                                        RenderPlot2D(parentCanvas);
                                    }
                                }
                                else if (parentWidget.Type == "plot3d")
                                {
                                    var tbl = value.Table;
                                    List<List<double>> grid = new List<List<double>>();
                                    for (int i = 1; i <= tbl.Length; i++)
                                    {
                                        var rowVal = tbl.Get(i);
                                        if (rowVal.Type == DataType.Table)
                                        {
                                            var rowList = new List<double>();
                                            for (int j = 1; j <= rowVal.Table.Length; j++)
                                            {
                                                rowList.Add(rowVal.Table.Get(j).Number);
                                            }
                                            grid.Add(rowList);
                                        }
                                    }
                                    widget.Plot3DData = grid;
                                    if (parentWidget.Element is Canvas parentCanvas)
                                    {
                                        RenderPlot3D(parentCanvas);
                                    }
                                }
                            }
                        }
                        else if (widget.Element is ProgressBar pb)
                        {
                            pb.Value = value.Number;
                        }
                        else if (widget.Element is Border colorBorder && widget.Type == "colorpicker")
                        {
                            var brush = ParseColor(value);
                            if (brush != null) colorBorder.Background = brush;
                        }
                        break;
                }
            });
        }

        public static string GetDisplayName(string fullName)
        {
            if (fullName.Contains("##"))
            {
                return fullName.Split(new[] { "##" }, StringSplitOptions.None)[0];
            }
            return fullName;
        }

        private static void ApplyNovaStyle(FrameworkElement element)
        {
            if (element is Control control)
            {
                control.FontFamily = new FontFamily("Inter, Segoe UI");
                control.FontSize = 11;
                control.Background = ThemeManager.TitleBarBgBrush;
                control.Foreground = ThemeManager.TitleBarFgBrush;
                control.BorderBrush = ThemeManager.BorderBrush;
                control.BorderThickness = new Thickness(1);
            }
            else if (element is TextBlock tb)
            {
                tb.FontFamily = new FontFamily("Inter, Segoe UI");
                tb.FontSize = 11;
                tb.Foreground = ThemeManager.TitleBarFgBrush;
            }
        }

        private static void HookWidgetEvents(GuiWidget widget)
        {
            var element = widget.Element;

            element.MouseEnter += (s, e) =>
            {
                if (widget.OnHover != null && CurrentScript != null)
                {
                    PrintLog("INFO", $"[GUI] Hovered: {widget.Name}");
                    CurrentScript.Call(widget.OnHover, DynValue.NewString(widget.Name));
                }
            };

            element.MouseDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    if (widget.OnDoubleClick != null && CurrentScript != null)
                    {
                        PrintLog("INFO", $"[GUI] DoubleClicked: {widget.Name}");
                        CurrentScript.Call(widget.OnDoubleClick, DynValue.NewString(widget.Name));
                    }
                }
                else
                {
                    if (!(element is Button) && !(element is Border && widget.Type == "colorpicker"))
                    {
                        if (widget.OnClick != null && CurrentScript != null)
                        {
                            PrintLog("INFO", $"[GUI] Clicked: {widget.Name}");
                            CurrentScript.Call(widget.OnClick, DynValue.NewString(widget.Name));
                        }
                    }
                }
            };

            if (element is Button btn)
            {
                btn.Click += (s, e) =>
                {
                    if (widget.OnClick != null && CurrentScript != null)
                    {
                        PrintLog("INFO", $"[GUI] Button Clicked: {widget.Name}");
                        CurrentScript.Call(widget.OnClick, DynValue.NewString(widget.Name));
                    }
                };
            }
            else if (element is Slider slider)
            {
                slider.ValueChanged += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewNumber(slider.Value));
                    }
                };
            }
            else if (element is CheckBox cb)
            {
                cb.Checked += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewBoolean(true));
                    }
                };
                cb.Unchecked += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewBoolean(false));
                    }
                };
            }
            else if (element is ComboBox combo)
            {
                combo.SelectionChanged += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        int idx = combo.SelectedIndex + 1;
                        CurrentScript.Call(widget.OnChanged, DynValue.NewNumber(idx));
                    }
                };
            }
            else if (element is TextBox tb)
            {
                tb.TextChanged += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewString(tb.Text));
                    }
                };
            }
            else if (element is Border colorBorder && widget.Type == "colorpicker")
            {
                colorBorder.MouseDown += (s, e) =>
                {
                    if (e.ClickCount == 1)
                    {
                        Color initial = Colors.Red;
                        if (colorBorder.Background is SolidColorBrush scb)
                        {
                            initial = scb.Color;
                        }

                        var picker = new ColorPickerDialog(initial);
                        var mainWin = Application.Current.MainWindow;
                        picker.Owner = mainWin;

                        if (picker.ShowDialog() == true)
                        {
                            var brush = new SolidColorBrush(picker.SelectedColor);
                            colorBorder.Background = brush;

                            if (widget.OnChanged != null && CurrentScript != null)
                            {
                                var tbl = new Table(CurrentScript);
                                tbl.Set(1, DynValue.NewNumber(picker.SelectedColor.R));
                                tbl.Set(2, DynValue.NewNumber(picker.SelectedColor.G));
                                tbl.Set(3, DynValue.NewNumber(picker.SelectedColor.B));
                                tbl.Set(4, DynValue.NewNumber(picker.SelectedColor.A));

                                CurrentScript.Call(widget.OnChanged, DynValue.NewTable(tbl));
                            }
                        }
                    }
                };
            }
        }

        private static void UpdatePanelLayout(GuiWidget panel)
        {
            if (panel.Type != "panel" || panel.ContainerCanvas == null) return;

            var children = new List<GuiWidget>();
            lock (_lock)
            {
                foreach (var w in _widgets.Values)
                {
                    if (w.ParentName == panel.Name)
                    {
                        children.Add(w);
                    }
                }
            }

            var childIndices = children.Select(c => new { Widget = c, Index = panel.ContainerCanvas.Children.IndexOf(c.Element) })
                                       .Where(x => x.Index >= 0)
                                       .OrderBy(x => x.Index)
                                       .Select(x => x.Widget)
                                       .ToList();

            double currentX = 10;
            double gap = 10;

            foreach (var child in childIndices)
            {
                Canvas.SetLeft(child.Element, currentX);
                double y = child.Position.Y;
                if (y == 0) y = 10;
                Canvas.SetTop(child.Element, y);
                child.Position = new Point(currentX, y);
                UpdateParentCanvasSize(child);

                double childWidth = child.Size.Width;
                if (double.IsNaN(childWidth) || childWidth <= 0)
                {
                    childWidth = child.Element.Width;
                }
                if (double.IsNaN(childWidth) || childWidth <= 0)
                {
                    childWidth = child.Element.ActualWidth;
                }
                if (double.IsNaN(childWidth) || childWidth <= 0)
                {
                    switch (child.Type.ToLower())
                    {
                        case "button": childWidth = 80; break;
                        case "label": childWidth = 80; break;
                        case "slider": childWidth = 120; break;
                        case "checkbox": childWidth = 120; break;
                        case "dropdown": childWidth = 100; break;
                        case "textinput": childWidth = 100; break;
                        case "progress": childWidth = 120; break;
                        case "colorpicker": childWidth = 40; break;
                        case "plot2d": childWidth = 200; break;
                        case "plot3d": childWidth = 200; break;
                        case "image": childWidth = 120; break;
                        case "panel": childWidth = 200; break;
                        default: childWidth = 100; break;
                    }
                }

                currentX += childWidth + gap;
            }
        }

        private static void UpdateParentCanvasSize(GuiWidget widget)
        {
            var parentCanvas = widget.Element.Parent as Canvas;
            if (parentCanvas != null)
            {
                double maxRight = 0;
                double maxBottom = 0;
                foreach (FrameworkElement child in parentCanvas.Children)
                {
                    double left = Canvas.GetLeft(child);
                    double top = Canvas.GetTop(child);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;

                    double w = double.IsNaN(child.Width) ? child.ActualWidth : child.Width;
                    double h = double.IsNaN(child.Height) ? child.ActualHeight : child.Height;

                    if (double.IsNaN(w) || w <= 0) w = 100; // fallback
                    if (double.IsNaN(h) || h <= 0) h = 30; // fallback

                    double r = left + w;
                    double b = top + h;

                    if (r > maxRight) maxRight = r;
                    if (b > maxBottom) maxBottom = b;
                }
                parentCanvas.Width = Math.Max(parentCanvas.Width, maxRight + 20);
                parentCanvas.Height = Math.Max(parentCanvas.Height, maxBottom + 20);
            }
        }

        private static Point? ParsePos(DynValue val)
        {
            if (val.Type != DataType.Table) return null;
            var tbl = val.Table;

            double x = 0;
            double y = 0;

            var xVal = tbl.Get("x");
            var yVal = tbl.Get("y");

            if (xVal.IsNotNil() && yVal.IsNotNil())
            {
                x = xVal.Number;
                y = yVal.Number;
            }
            else if (tbl.Length >= 2)
            {
                x = tbl.Get(1).Number;
                y = tbl.Get(2).Number;
            }
            else
            {
                return null;
            }
            return new Point(x, y);
        }

        private static Size? ParseSize(DynValue val)
        {
            if (val.Type != DataType.Table) return null;
            var tbl = val.Table;

            double w = 0;
            double h = 0;

            var wVal = tbl.Get("width");
            var hVal = tbl.Get("height");

            if (wVal.IsNotNil() && hVal.IsNotNil())
            {
                w = wVal.Number;
                h = hVal.Number;
            }
            else if (tbl.Length >= 2)
            {
                w = tbl.Get(1).Number;
                h = tbl.Get(2).Number;
            }
            else
            {
                return null;
            }
            return new Size(w, h);
        }

        private static Brush? ParseColor(DynValue val)
        {
            if (val.Type != DataType.Table) return null;
            var tbl = val.Table;
            if (tbl.Length < 3) return null;

            byte r = (byte)(tbl.Get(1).Number);
            byte g = (byte)(tbl.Get(2).Number);
            byte b = (byte)(tbl.Get(3).Number);
            byte a = tbl.Length >= 4 ? (byte)(tbl.Get(4).Number) : (byte)255;

            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        private static void PrintLog(string level, string message)
        {
            var mainWin = Application.Current.Dispatcher.Invoke(() => Application.Current.MainWindow as MainWindow);
            if (mainWin != null)
            {
                _ = mainWin.SendToHostAsync(new
                {
                    type = "LOG_PRINTED",
                    payload = new { level = level, message = message }
                });
            }
        }

        private static void RenderPlot2D(Canvas canvas)
        {
            canvas.ClipToBounds = true;
            canvas.Children.Clear();

            double width = canvas.Width;
            double height = canvas.Height;
            if (double.IsNaN(width) || width <= 0) width = 200;
            if (double.IsNaN(height) || height <= 0) height = 150;

            var bg = new Border
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderBrush = ThemeManager.BorderBrush,
                BorderThickness = new Thickness(1)
            };
            canvas.Children.Add(bg);

            for (int i = 1; i < 4; i++)
            {
                var gridLine = new Line
                {
                    X1 = 0,
                    Y1 = height * i / 4.0,
                    X2 = width,
                    Y2 = height * i / 4.0,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(gridLine);
            }

            string plotName = string.Empty;
            lock (_lock)
            {
                foreach (var pair in _widgets)
                {
                    if (pair.Value.Element == canvas)
                    {
                        plotName = pair.Key;
                        break;
                    }
                }
            }

            var lines = GetChildPlotLines(plotName);

            List<List<double>> datasets = new List<List<double>>();
            List<string> legends = new List<string>();
            List<Brush> colors = new List<Brush>();

            if (lines.Count > 0)
            {
                for (int idx = 0; idx < lines.Count; idx++)
                {
                    var lineWidget = lines[idx];
                    if (lineWidget.Plot2DData.Count > 0)
                    {
                        datasets.Add(lineWidget.Plot2DData);
                        legends.Add(!string.IsNullOrEmpty(lineWidget.Legend) ? lineWidget.Legend : GetDisplayName(lineWidget.Name));
                        colors.Add(lineWidget.CustomColor ?? SeriesColors[idx % SeriesColors.Length]);
                    }
                }
            }
            else
            {
                if (canvas.Tag is Plot2DState plotState && plotState.Data.Count > 0)
                {
                    datasets.Add(plotState.Data);
                    legends.Add(plotState.Legend);
                    colors.Add(ThemeManager.AccentBrush);
                }
            }

            if (datasets.Count == 0) return;

            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (var dataset in datasets)
            {
                foreach (var val in dataset)
                {
                    if (val < min) min = val;
                    if (val > max) max = val;
                }
            }
            double range = max - min;
            if (range == 0) range = 1;

            for (int d = 0; d < datasets.Count; d++)
            {
                var dataset = datasets[d];
                var brush = colors[d];

                var polyline = new Polyline
                {
                    Stroke = brush,
                    StrokeThickness = 2
                };

                for (int i = 0; i < dataset.Count; i++)
                {
                    double x = width * i / Math.Max(1, dataset.Count - 1);
                    double y = height - (height * (dataset[i] - min) / range);
                    polyline.Points.Add(new Point(x, y));
                }

                canvas.Children.Add(polyline);
            }

            bool hasAnyLegend = legends.Any(l => !string.IsNullOrEmpty(l));
            if (hasAnyLegend)
            {
                var legendPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };

                for (int d = 0; d < datasets.Count; d++)
                {
                    string legText = legends[d];
                    if (string.IsNullOrEmpty(legText)) continue;

                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
                    
                    var accentLine = new Line
                    {
                        X1 = 0,
                        Y1 = 0,
                        X2 = 12,
                        Y2 = 0,
                        Stroke = colors[d],
                        StrokeThickness = 2,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    itemPanel.Children.Add(accentLine);

                    var legendLabel = new TextBlock
                    {
                        Text = legText,
                        Foreground = ThemeManager.TitleBarFgBrush,
                        FontSize = 9,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    itemPanel.Children.Add(legendLabel);
                    legendPanel.Children.Add(itemPanel);
                }

                var legendBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(200, 17, 17, 27)),
                    BorderBrush = ThemeManager.BorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 4, 6, 4),
                    Child = legendPanel
                };

                Canvas.SetTop(legendBorder, 6);
                Canvas.SetRight(legendBorder, 6);
                canvas.Children.Add(legendBorder);
            }
        }

        private static void RenderPlot3D(Canvas canvas)
        {
            canvas.ClipToBounds = true;
            canvas.Children.Clear();

            double width = canvas.Width;
            double height = canvas.Height;
            if (double.IsNaN(width) || width <= 0) width = 200;
            if (double.IsNaN(height) || height <= 0) height = 150;

            Plot3DState? state = canvas.Tag as Plot3DState;
            if (state == null)
            {
                state = new Plot3DState();
                canvas.Tag = state;

                canvas.MouseRightButtonDown += (s, e) =>
                {
                    state.IsDragging = true;
                    state.LastMousePos = e.GetPosition(canvas);
                    canvas.CaptureMouse();
                };

                canvas.MouseRightButtonUp += (s, e) =>
                {
                    state.IsDragging = false;
                    canvas.ReleaseMouseCapture();
                };

                canvas.MouseMove += (s, e) =>
                {
                    if (state.IsDragging)
                    {
                        var pos = e.GetPosition(canvas);
                        double dx = pos.X - state.LastMousePos.X;
                        double dy = pos.Y - state.LastMousePos.Y;

                        state.RotateX += dx * 0.5;
                        state.RotateY -= dy * 0.5;

                        state.LastMousePos = pos;
                        RenderPlot3D(canvas);
                    }
                };

                canvas.MouseWheel += (s, e) =>
                {
                    if (e.Delta > 0)
                        state.Zoom *= 1.1;
                    else
                        state.Zoom /= 1.1;

                    if (state.Zoom < 0.1) state.Zoom = 0.1;
                    if (state.Zoom > 10.0) state.Zoom = 10.0;

                    e.Handled = true;
                    RenderPlot3D(canvas);
                };
            }

            var bg = new Border
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderBrush = ThemeManager.BorderBrush,
                BorderThickness = new Thickness(1)
            };
            canvas.Children.Add(bg);

            string plotName = string.Empty;
            lock (_lock)
            {
                foreach (var pair in _widgets)
                {
                    if (pair.Value.Element == canvas)
                    {
                        plotName = pair.Key;
                        break;
                    }
                }
            }

            var lines = GetChildPlotLines(plotName);

            List<List<List<double>>> datasets = new List<List<List<double>>>();
            List<string> legends = new List<string>();
            List<Brush> colors = new List<Brush>();

            if (lines.Count > 0)
            {
                for (int idx = 0; idx < lines.Count; idx++)
                {
                    var lineWidget = lines[idx];
                    if (lineWidget.Plot3DData.Count > 0 && lineWidget.Plot3DData[0].Count > 0)
                    {
                        datasets.Add(lineWidget.Plot3DData);
                        legends.Add(!string.IsNullOrEmpty(lineWidget.Legend) ? lineWidget.Legend : GetDisplayName(lineWidget.Name));
                        colors.Add(lineWidget.CustomColor ?? SeriesColors[idx % SeriesColors.Length]);
                    }
                }
            }
            else
            {
                if (state.GridData.Count > 0 && state.GridData[0].Count > 0)
                {
                    datasets.Add(state.GridData);
                    legends.Add(state.Legend);
                    colors.Add(ThemeManager.AccentBrush);
                }
            }

            if (datasets.Count == 0) return;

            double minZ = double.MaxValue;
            double maxZ = double.MinValue;
            foreach (var grid in datasets)
            {
                foreach (var row in grid)
                {
                    foreach (var val in row)
                    {
                        if (val < minZ) minZ = val;
                        if (val > maxZ) maxZ = val;
                    }
                }
            }
            double rangeZ = maxZ - minZ;
            if (rangeZ == 0) rangeZ = 1;

            double radX = state.RotateX * Math.PI / 180.0;
            double radY = state.RotateY * Math.PI / 180.0;
            double cosX = Math.Cos(radX);
            double sinX = Math.Sin(radX);
            double cosY = Math.Cos(radY);
            double sinY = Math.Sin(radY);

            for (int d = 0; d < datasets.Count; d++)
            {
                var grid = datasets[d];
                var strokeBrush = colors[d];
                int rows = grid.Count;
                int cols = grid[0].Count;

                Point Project(int r, int c, double val)
                {
                    double x = (double)c / Math.Max(1, cols - 1) - 0.5;
                    double y = (double)r / Math.Max(1, rows - 1) - 0.5;
                    double z = (val - minZ) / rangeZ - 0.5;

                    double y1 = y * cosY - z * sinY;
                    double z1 = y * sinY + z * cosY;

                    double x2 = x * cosX - y1 * sinX;
                    double y2 = x * sinX + y1 * cosX;

                    double scale = width * 0.7 * state.Zoom;
                    double screenX = width / 2.0 + x2 * scale;
                    double screenY = height / 2.0 + y2 * scale - z1 * (scale * 0.5);

                    return new Point(screenX, screenY);
                }

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols - 1; c++)
                    {
                        var pt1 = Project(r, c, grid[r][c]);
                        var pt2 = Project(r, c + 1, grid[r][c + 1]);
                        var line = new Line
                        {
                            X1 = pt1.X,
                            Y1 = pt1.Y,
                            X2 = pt2.X,
                            Y2 = pt2.Y,
                            Stroke = strokeBrush,
                            StrokeThickness = 1
                        };
                        canvas.Children.Add(line);
                    }
                }

                for (int c = 0; c < cols; c++)
                {
                    for (int r = 0; r < rows - 1; r++)
                    {
                        var pt1 = Project(r, c, grid[r][c]);
                        var pt2 = Project(r + 1, c, grid[r + 1][c]);
                        var line = new Line
                        {
                            X1 = pt1.X,
                            Y1 = pt1.Y,
                            X2 = pt2.X,
                            Y2 = pt2.Y,
                            Stroke = strokeBrush,
                            StrokeThickness = 1
                        };
                        canvas.Children.Add(line);
                    }
                }
            }

            bool hasAnyLegend = legends.Any(l => !string.IsNullOrEmpty(l));
            if (hasAnyLegend)
            {
                var legendPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };

                for (int d = 0; d < datasets.Count; d++)
                {
                    string legText = legends[d];
                    if (string.IsNullOrEmpty(legText)) continue;

                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
                    
                    var accentMarker = new Rectangle
                    {
                        Width = 8,
                        Height = 8,
                        Fill = colors[d],
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    itemPanel.Children.Add(accentMarker);

                    var legendLabel = new TextBlock
                    {
                        Text = legText,
                        Foreground = ThemeManager.TitleBarFgBrush,
                        FontSize = 9,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    itemPanel.Children.Add(legendLabel);
                    legendPanel.Children.Add(itemPanel);
                }

                var legendBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(200, 17, 17, 27)),
                    BorderBrush = ThemeManager.BorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 4, 6, 4),
                    Child = legendPanel
                };

                Canvas.SetTop(legendBorder, 6);
                Canvas.SetRight(legendBorder, 6);
                canvas.Children.Add(legendBorder);
            }
        }
    }

    public class ColorPickerDialog : Window
    {
        private readonly Slider _sR;
        private readonly Slider _sG;
        private readonly Slider _sB;
        private readonly Border _preview;
        public Color SelectedColor { get; private set; }

        public ColorPickerDialog(Color initialColor)
        {
            Title = "Select Color";
            Width = 280;
            Height = 240;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = ThemeManager.PanelBgBrush;
            Foreground = ThemeManager.TitleBarFgBrush;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;

            var mainGrid = new Grid { Margin = new Thickness(10) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var slidersGrid = new Grid();
            slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            slidersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            slidersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            slidersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            slidersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            slidersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblR = new TextBlock { Text = "R", Foreground = Brushes.Red, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lblR, 0); Grid.SetColumn(lblR, 0); slidersGrid.Children.Add(lblR);
            _sR = new Slider { Minimum = 0, Maximum = 255, Value = initialColor.R, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(_sR, 0); Grid.SetColumn(_sR, 1); slidersGrid.Children.Add(_sR);

            var lblG = new TextBlock { Text = "G", Foreground = Brushes.Green, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lblG, 1); Grid.SetColumn(lblG, 0); slidersGrid.Children.Add(lblG);
            _sG = new Slider { Minimum = 0, Maximum = 255, Value = initialColor.G, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(_sG, 1); Grid.SetColumn(_sG, 1); slidersGrid.Children.Add(_sG);

            var lblB = new TextBlock { Text = "B", Foreground = Brushes.DodgerBlue, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lblB, 2); Grid.SetColumn(lblB, 0); slidersGrid.Children.Add(lblB);
            _sB = new Slider { Minimum = 0, Maximum = 255, Value = initialColor.B, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(_sB, 2); Grid.SetColumn(_sB, 1); slidersGrid.Children.Add(_sB);

            _preview = new Border { Height = 40, Margin = new Thickness(5), BorderBrush = ThemeManager.BorderBrush, BorderThickness = new Thickness(1) };
            Grid.SetRow(_preview, 3); Grid.SetColumnSpan(_preview, 2); slidersGrid.Children.Add(_preview);

            void UpdatePreview()
            {
                var color = Color.FromRgb((byte)_sR.Value, (byte)_sG.Value, (byte)_sB.Value);
                _preview.Background = new SolidColorBrush(color);
            }

            _sR.ValueChanged += (s, e) => UpdatePreview();
            _sG.ValueChanged += (s, e) => UpdatePreview();
            _sB.ValueChanged += (s, e) => UpdatePreview();
            UpdatePreview();

            Grid.SetRow(slidersGrid, 0);
            mainGrid.Children.Add(slidersGrid);

            var btnOk = new Button { Content = "OK", Margin = new Thickness(5), Height = 25, Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
            btnOk.Background = ThemeManager.TitleBarBgBrush;
            btnOk.Foreground = ThemeManager.TitleBarFgBrush;
            btnOk.Click += (s, e) =>
            {
                SelectedColor = Color.FromRgb((byte)_sR.Value, (byte)_sG.Value, (byte)_sB.Value);
                DialogResult = true;
                Close();
            };
            Grid.SetRow(btnOk, 1);
            mainGrid.Children.Add(btnOk);

            Content = mainGrid;
        }
    }

    public class Plot2DState
    {
        public List<double> Data { get; set; } = new List<double>();
        public string Legend { get; set; } = string.Empty;
    }

    public class Plot3DState
    {
        public double RotateX { get; set; } = 45.0;
        public double RotateY { get; set; } = 30.0;
        public Point LastMousePos { get; set; }
        public bool IsDragging { get; set; }
        public List<List<double>> GridData { get; set; } = new List<List<double>>();
        public double Zoom { get; set; } = 1.0;
        public string Legend { get; set; } = string.Empty;
    }
}

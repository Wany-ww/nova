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
        public string PlotType { get; set; } = "line";

        // Advanced styling properties
        public List<double> PlotXData { get; set; } = new List<double>();
        public List<double> PlotYData { get; set; } = new List<double>();
        public List<double> PlotZData { get; set; } = new List<double>();

        public double LineThickness { get; set; } = 2.0;
        public string LineStyle { get; set; } = "solid"; // "solid", "dashed", "dotted"

        public Brush? MarkerColor { get; set; }
        public double MarkerSize { get; set; } = 6.0;
        public string MarkerStyle { get; set; } = "circle"; // "circle", "square", "triangle"

        public Brush? BarColor { get; set; }
        public double BarWidthValue { get; set; } = -1.0;
        public string BarStyle { get; set; } = "solid"; // "solid", "gradient"

        // Wrapping and hover customization
        public FrameworkElement InnerElement { get; set; } = null!;
        public double BorderRadius { get; set; } = 0.0;
        public Brush? HoverColor { get; set; }
        public Brush? OriginalBackground { get; set; }
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


        private static ControlTemplate CreateSimpleButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            
            var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            borderFactory.AppendChild(cpFactory);
            template.VisualTree = borderFactory;
            return template;
        }

        private static Brush CreateFrozenBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        private static readonly Brush[] SeriesColors = new Brush[]
        {
            ThemeManager.AccentBrush,
            CreateFrozenBrush("#f5c2e7"), // Pink
            CreateFrozenBrush("#a6e3a1"), // Green
            CreateFrozenBrush("#fab387"), // Peach
            CreateFrozenBrush("#b4befe"), // Lavender
            CreateFrozenBrush("#f38ba8"), // Red
            CreateFrozenBrush("#f9e2af"), // Yellow
            CreateFrozenBrush("#94e2d5")  // Teal
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
                            parentCanvas = parentWidget.InnerElement as Canvas;
                        }
                    }

                    if (parentCanvas == null) return;

                    FrameworkElement? element = null;
                    FrameworkElement? innerElement = null;
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
                                Child = containerCanvas,
                                CornerRadius = new CornerRadius(0)
                            };
                            innerElement = element;
                            break;

                        case "colorpicker":
                            element = new Border
                            {
                                Width = 40,
                                Height = 22,
                                Background = Brushes.Red,
                                BorderThickness = new Thickness(1),
                                BorderBrush = ThemeManager.BorderBrush,
                                CornerRadius = new CornerRadius(0)
                            };
                            innerElement = element;
                            break;

                        case "plotline":
                            element = new FrameworkElement();
                            innerElement = element;
                            break;

                        default:
                            FrameworkElement control = null!;
                            switch (type)
                            {
                                case "button":
                                    control = new Button { Content = displayName, Width = 80, Height = 22 };
                                    break;
                                case "label":
                                    control = new TextBlock { Text = displayName };
                                    break;
                                case "slider":
                                    control = new Slider { Minimum = 0, Maximum = 100, Value = 0, Width = 120, Height = 22 };
                                    break;
                                case "checkbox":
                                    control = new CheckBox { Content = displayName };
                                    break;
                                case "dropdown":
                                    control = new ComboBox { Width = 100, Height = 22 };
                                    break;
                                case "textinput":
                                    control = new TextBox { Width = 100, Height = 22 };
                                    break;
                                case "image":
                                    control = new Image { Stretch = Stretch.Uniform, Width = 150, Height = 150 };
                                    break;
                                case "plot2d":
                                    control = new Canvas { Width = 200, Height = 150 };
                                    break;
                                case "plot3d":
                                    control = new Canvas { Width = 200, Height = 150 };
                                    break;
                                case "progress":
                                    control = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Width = 120, Height = 15 };
                                    break;
                                case "radiobutton":
                                    control = new RadioButton { Content = displayName, GroupName = parent };
                                    break;
                                case "textarea":
                                    control = new TextBox
                                    {
                                        AcceptsReturn = true,
                                        TextWrapping = TextWrapping.Wrap,
                                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                        Width = 150,
                                        Height = 60
                                    };
                                    break;
                                default:
                                    return;
                            }

                            // Wrap in a border wrapper
                            var borderWrapper = new Border
                            {
                                Background = Brushes.Transparent,
                                BorderBrush = Brushes.Transparent,
                                BorderThickness = new Thickness(0),
                                CornerRadius = new CornerRadius(0),
                                Child = control
                            };

                            // Move sizes from control to wrapper
                            if (!double.IsNaN(control.Width) && control.Width > 0)
                            {
                                borderWrapper.Width = control.Width;
                                if (!(control is Canvas) && !(control is Image))
                                {
                                    control.Width = double.NaN;
                                    control.HorizontalAlignment = HorizontalAlignment.Stretch;
                                }
                            }
                            if (!double.IsNaN(control.Height) && control.Height > 0)
                            {
                                borderWrapper.Height = control.Height;
                                if (!(control is Canvas) && !(control is Image))
                                {
                                    control.Height = double.NaN;
                                    control.VerticalAlignment = VerticalAlignment.Stretch;
                                }
                            }

                            element = borderWrapper;
                            innerElement = control;
                            break;
                    }

                    parentCanvas.Children.Add(element);
                    Canvas.SetLeft(element, 0);
                    Canvas.SetTop(element, 0);

                    var widget = new GuiWidget
                    {
                        Name = name,
                        Type = type,
                        ParentName = parent,
                        Element = element,
                        InnerElement = innerElement,
                        ContainerCanvas = containerCanvas,
                        Position = new Point(0, 0),
                        Size = new Size(double.IsNaN(element.Width) ? double.NaN : element.Width, double.IsNaN(element.Height) ? double.NaN : element.Height),
                        Legend = GetDisplayName(name)
                    };

                    ApplyNovaStyle(widget);

                    HookWidgetEvents(widget);

                    _widgets[name] = widget;
                    if (parentDialog != null)
                    {
                        parentDialog.Widgets[name] = widget;
                    }

                    if (widget.Type == "plotline" && parentWidget != null)
                    {
                        if (parentWidget.Type == "plot2d" && parentCanvas is Canvas canvas2d) RenderPlot2D(canvas2d);
                        else if (parentWidget.Type == "plot3d" && parentCanvas is Canvas canvas3d) RenderPlot3D(canvas3d);
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
                            if (widget.InnerElement != widget.Element)
                            {
                                widget.InnerElement.Width = size.Value.Width;
                                widget.InnerElement.Height = size.Value.Height;
                            }
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

                    case "border_radius":
                        widget.BorderRadius = value.Number;
                        if (widget.Element is Border border)
                        {
                            border.CornerRadius = new CornerRadius(widget.BorderRadius);
                        }
                        break;

                    case "hover_color":
                        widget.HoverColor = ParseColor(value);
                        break;

                    case "horizontal":
                        if (widget.Type == "panel")
                        {
                            widget.IsHorizontal = value.CastToBool();
                            UpdatePanelLayout(widget);
                        }
                        break;

                    case "legend":
                        if (widget.Type == "plot2d" && widget.InnerElement is Canvas plotCanvas2d)
                        {
                            if (!(plotCanvas2d.Tag is Plot2DState state2d))
                            {
                                state2d = new Plot2DState();
                                plotCanvas2d.Tag = state2d;
                            }
                            state2d.Legend = value.CastToString() ?? string.Empty;
                            RenderPlot2D(plotCanvas2d);
                        }
                        else if (widget.Type == "plot3d" && widget.InnerElement is Canvas plotCanvas3d)
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
                            if (parentWidget != null && parentWidget.InnerElement is Canvas parentCanvas)
                            {
                                if (parentWidget.Type == "plot2d") RenderPlot2D(parentCanvas);
                                else if (parentWidget.Type == "plot3d") RenderPlot3D(parentCanvas);
                            }
                        }
                        break;

                    case "legend_text_color":
                        if (widget.Type == "plot2d" && widget.InnerElement is Canvas plotCanvasColor2d)
                        {
                            if (!(plotCanvasColor2d.Tag is Plot2DState state2d))
                            {
                                state2d = new Plot2DState();
                                plotCanvasColor2d.Tag = state2d;
                            }
                            state2d.LegendTextColor = ParseColor(value);
                            RenderPlot2D(plotCanvasColor2d);
                        }
                        else if (widget.Type == "plot3d" && widget.InnerElement is Canvas plotCanvasColor3d)
                        {
                            if (!(plotCanvasColor3d.Tag is Plot3DState state3d))
                            {
                                state3d = new Plot3DState();
                                plotCanvasColor3d.Tag = state3d;
                            }
                            state3d.LegendTextColor = ParseColor(value);
                            RenderPlot3D(plotCanvasColor3d);
                        }
                        break;

                    case "legend_background_color":
                    case "legend_bg_color":
                        if (widget.Type == "plot2d" && widget.InnerElement is Canvas plotCanvasBg2d)
                        {
                            if (!(plotCanvasBg2d.Tag is Plot2DState state2d))
                            {
                                state2d = new Plot2DState();
                                plotCanvasBg2d.Tag = state2d;
                            }
                            state2d.LegendBgColor = ParseColor(value);
                            RenderPlot2D(plotCanvasBg2d);
                        }
                        else if (widget.Type == "plot3d" && widget.InnerElement is Canvas plotCanvasBg3d)
                        {
                            if (!(plotCanvasBg3d.Tag is Plot3DState state3d))
                            {
                                state3d = new Plot3DState();
                                plotCanvasBg3d.Tag = state3d;
                            }
                            state3d.LegendBgColor = ParseColor(value);
                            RenderPlot3D(plotCanvasBg3d);
                        }
                        break;

                    case "title":
                    case "title_font_size":
                    case "title_color":
                    case "title_background_color":
                    case "title_visible":
                        if (widget.Type == "plot2d" && widget.InnerElement is Canvas c2dTitle)
                        {
                            if (!(c2dTitle.Tag is Plot2DState s2t)) { s2t = new Plot2DState(); c2dTitle.Tag = s2t; }
                            var k = key.ToLower();
                            if (k == "title") s2t.Title = value.CastToString() ?? string.Empty;
                            else if (k == "title_font_size") s2t.TitleFontSize = value.Number;
                            else if (k == "title_color") s2t.TitleColor = ParseColor(value);
                            else if (k == "title_background_color") s2t.TitleBackground = ParseColor(value);
                            else if (k == "title_visible") s2t.TitleVisible = value.CastToBool();
                            RenderPlot2D(c2dTitle);
                        }
                        else if (widget.Type == "plot3d" && widget.InnerElement is Canvas c3dTitle)
                        {
                            if (!(c3dTitle.Tag is Plot3DState s3t)) { s3t = new Plot3DState(); c3dTitle.Tag = s3t; }
                            var k = key.ToLower();
                            if (k == "title") s3t.Title = value.CastToString() ?? string.Empty;
                            else if (k == "title_font_size") s3t.TitleFontSize = value.Number;
                            else if (k == "title_color") s3t.TitleColor = ParseColor(value);
                            else if (k == "title_background_color") s3t.TitleBackground = ParseColor(value);
                            else if (k == "title_visible") s3t.TitleVisible = value.CastToBool();
                            RenderPlot3D(c3dTitle);
                        }
                        break;

                    case "grid_visible_x":
                    case "grid_visible_y":
                    case "grid_visible_z":
                    case "grid_interval_x":
                    case "grid_interval_y":
                    case "grid_interval_z":
                    case "grid_color_x":
                    case "grid_color_y":
                    case "grid_color_z":
                    case "grid_thickness_x":
                    case "grid_thickness_y":
                    case "grid_thickness_z":
                        if (widget.Type == "plot2d" && widget.InnerElement is Canvas g2d)
                        {
                            if (!(g2d.Tag is Plot2DState s2g)) { s2g = new Plot2DState(); g2d.Tag = s2g; }
                            var k = key.ToLower();
                            if (k == "grid_visible_x") s2g.GridVisibleX = value.CastToBool();
                            else if (k == "grid_visible_y") s2g.GridVisibleY = value.CastToBool();
                            else if (k == "grid_interval_x") s2g.GridIntervalX = value.Number;
                            else if (k == "grid_interval_y") s2g.GridIntervalY = value.Number;
                            else if (k == "grid_color_x") s2g.GridColorX = ParseColor(value);
                            else if (k == "grid_color_y") s2g.GridColorY = ParseColor(value);
                            else if (k == "grid_thickness_x") s2g.GridThicknessX = value.Number;
                            else if (k == "grid_thickness_y") s2g.GridThicknessY = value.Number;
                            RenderPlot2D(g2d);
                        }
                        else if (widget.Type == "plot3d" && widget.InnerElement is Canvas g3d)
                        {
                            if (!(g3d.Tag is Plot3DState s3g)) { s3g = new Plot3DState(); g3d.Tag = s3g; }
                            var k = key.ToLower();
                            if (k == "grid_visible_x") s3g.GridVisibleX = value.CastToBool();
                            else if (k == "grid_visible_y") s3g.GridVisibleY = value.CastToBool();
                            else if (k == "grid_visible_z") s3g.GridVisibleZ = value.CastToBool();
                            else if (k == "grid_interval_x") s3g.GridIntervalX = value.Number;
                            else if (k == "grid_interval_y") s3g.GridIntervalY = value.Number;
                            else if (k == "grid_interval_z") s3g.GridIntervalZ = value.Number;
                            else if (k == "grid_color_x") s3g.GridColorX = ParseColor(value);
                            else if (k == "grid_color_y") s3g.GridColorY = ParseColor(value);
                            else if (k == "grid_color_z") s3g.GridColorZ = ParseColor(value);
                            else if (k == "grid_thickness_x") s3g.GridThicknessX = value.Number;
                            else if (k == "grid_thickness_y") s3g.GridThicknessY = value.Number;
                            else if (k == "grid_thickness_z") s3g.GridThicknessZ = value.Number;
                            RenderPlot3D(g3d);
                        }
                        break;

                    case "range_x":
                    case "range_y":
                    case "range_z":
                    case "range_min_x":
                    case "range_max_x":
                    case "range_min_y":
                    case "range_max_y":
                    case "range_min_z":
                    case "range_max_z":
                    case "tick_interval_x":
                    case "tick_interval_y":
                    case "tick_interval_z":
                    case "snaps_orientation":
                    case "snaps_text_color":
                    case "snaps_background_color":
                        if (widget.Type == "plot2d" && widget.InnerElement is Canvas r2d)
                        {
                            if (!(r2d.Tag is Plot2DState s2r)) { s2r = new Plot2DState(); r2d.Tag = s2r; }
                            var k = key.ToLower();
                            if (k == "range_x" && value.Type == DataType.Table && value.Table.Length >= 2)
                            {
                                s2r.RangeMinX = value.Table.Get(1).Number;
                                s2r.RangeMaxX = value.Table.Get(2).Number;
                            }
                            else if (k == "range_y" && value.Type == DataType.Table && value.Table.Length >= 2)
                            {
                                s2r.RangeMinY = value.Table.Get(1).Number;
                                s2r.RangeMaxY = value.Table.Get(2).Number;
                            }
                            else if (k == "range_min_x") s2r.RangeMinX = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_max_x") s2r.RangeMaxX = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_min_y") s2r.RangeMinY = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_max_y") s2r.RangeMaxY = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "tick_interval_x") s2r.TickIntervalX = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "tick_interval_y") s2r.TickIntervalY = value.Type == DataType.Nil ? (double?)null : value.Number;
                            RenderPlot2D(r2d);
                        }
                        else if (widget.Type == "plot3d" && widget.InnerElement is Canvas r3d)
                        {
                            if (!(r3d.Tag is Plot3DState s3r)) { s3r = new Plot3DState(); r3d.Tag = s3r; }
                            var k = key.ToLower();
                            if (k == "range_x" && value.Type == DataType.Table && value.Table.Length >= 2)
                            {
                                s3r.RangeMinX = value.Table.Get(1).Number;
                                s3r.RangeMaxX = value.Table.Get(2).Number;
                            }
                            else if (k == "range_y" && value.Type == DataType.Table && value.Table.Length >= 2)
                            {
                                s3r.RangeMinY = value.Table.Get(1).Number;
                                s3r.RangeMaxY = value.Table.Get(2).Number;
                            }
                            else if (k == "range_z" && value.Type == DataType.Table && value.Table.Length >= 2)
                            {
                                s3r.RangeMinZ = value.Table.Get(1).Number;
                                s3r.RangeMaxZ = value.Table.Get(2).Number;
                            }
                            else if (k == "range_min_x") s3r.RangeMinX = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_max_x") s3r.RangeMaxX = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_min_y") s3r.RangeMinY = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_max_y") s3r.RangeMaxY = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_min_z") s3r.RangeMinZ = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "range_max_z") s3r.RangeMaxZ = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "tick_interval_x") s3r.TickIntervalX = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "tick_interval_y") s3r.TickIntervalY = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "tick_interval_z") s3r.TickIntervalZ = value.Type == DataType.Nil ? (double?)null : value.Number;
                            else if (k == "snaps_orientation") s3r.SnapsOrientation = value.CastToString() ?? "horizontal";
                            else if (k == "snaps_text_color") s3r.SnapsTextColor = ParseColor(value);
                            else if (k == "snaps_background_color") s3r.SnapsBgColor = ParseColor(value);
                            RenderPlot3D(r3d);
                        }
                        break;

                    case "line_color":
                    case "line_thickness":
                    case "line_style":
                    case "marker_color":
                    case "marker_size":
                    case "marker_style":
                    case "bar_color":
                    case "bar_width":
                    case "bar_style":
                        if (widget.Type == "plotline")
                        {
                            var k = key.ToLower();
                            if (k == "line_color") widget.CustomColor = ParseColor(value);
                            else if (k == "line_thickness") widget.LineThickness = value.Number;
                            else if (k == "line_style") widget.LineStyle = value.CastToString() ?? "solid";
                            else if (k == "marker_color") widget.MarkerColor = ParseColor(value);
                            else if (k == "marker_size") widget.MarkerSize = value.Number;
                            else if (k == "marker_style") widget.MarkerStyle = value.CastToString() ?? "circle";
                            else if (k == "bar_color") widget.BarColor = ParseColor(value);
                            else if (k == "bar_width") widget.BarWidthValue = value.Number;
                            else if (k == "bar_style") widget.BarStyle = value.CastToString() ?? "solid";

                            GuiWidget? parentWidget = null;
                            lock (_lock)
                            {
                                _widgets.TryGetValue(widget.ParentName, out parentWidget);
                            }
                            if (parentWidget != null && parentWidget.InnerElement is Canvas parentCanvas)
                            {
                                if (parentWidget.Type == "plot2d") RenderPlot2D(parentCanvas);
                                else if (parentWidget.Type == "plot3d") RenderPlot3D(parentCanvas);
                            }
                        }
                        break;

                    case "data_x":
                    case "data_y":
                    case "data_z":
                        if (widget.Type == "plotline" && value.Type == DataType.Table)
                        {
                            var tbl = value.Table;
                            var pts = new List<double>();
                            for (int i = 1; i <= tbl.Length; i++) pts.Add(tbl.Get(i).Number);

                            var k = key.ToLower();
                            if (k == "data_x") widget.PlotXData = pts;
                            else if (k == "data_y") widget.PlotYData = pts;
                            else if (k == "data_z") widget.PlotZData = pts;

                            GuiWidget? parentWidget = null;
                            lock (_lock)
                            {
                                _widgets.TryGetValue(widget.ParentName, out parentWidget);
                            }
                            if (parentWidget != null && parentWidget.InnerElement is Canvas parentCanvas)
                            {
                                if (parentWidget.Type == "plot2d") RenderPlot2D(parentCanvas);
                                else if (parentWidget.Type == "plot3d") RenderPlot3D(parentCanvas);
                            }
                        }
                        break;

                    case "plot_type":
                        if (widget.Type == "plotline")
                        {
                            widget.PlotType = value.CastToString() ?? "line";
                            GuiWidget? parentWidget = null;
                            lock (_lock)
                            {
                                _widgets.TryGetValue(widget.ParentName, out parentWidget);
                            }
                            if (parentWidget != null && parentWidget.InnerElement is Canvas parentCanvas)
                            {
                                if (parentWidget.Type == "plot2d") RenderPlot2D(parentCanvas);
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
                            if (widget.InnerElement is Control control) control.Foreground = fgBrush;
                            else if (widget.InnerElement is TextBlock tb) tb.Foreground = fgBrush;
                            else if (widget.Type == "plotline")
                            {
                                widget.CustomColor = fgBrush;
                                GuiWidget? parentWidget = null;
                                lock (_lock)
                                {
                                    _widgets.TryGetValue(widget.ParentName, out parentWidget);
                                }
                                if (parentWidget != null && parentWidget.InnerElement is Canvas parentCanvas)
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
                            widget.OriginalBackground = bgBrush;
                            if (widget.Element is Border borderWrapper) borderWrapper.Background = bgBrush;
                            else if (widget.Element is Panel panelControl) panelControl.Background = bgBrush;
                            else if (widget.InnerElement is Control control) control.Background = bgBrush;
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
                            if (widget.InnerElement is Button btn) btn.Content = displayLabel;
                            else if (widget.InnerElement is TextBlock textBlock) textBlock.Text = displayLabel;
                            else if (widget.InnerElement is CheckBox cb) cb.Content = displayLabel;
                            else if (widget.InnerElement is RadioButton rb) rb.Content = displayLabel;
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
                        if (widget.InnerElement is ComboBox combo && value.Type == DataType.Table)
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
                        else if (widget.InnerElement is CheckBox checkBox)
                        {
                            checkBox.IsChecked = value.Boolean;
                        }
                        else if (widget.InnerElement is RadioButton rbVal)
                        {
                            rbVal.IsChecked = value.Boolean;
                        }
                        else if (widget.InnerElement is ComboBox com)
                        {
                            int idx = (int)value.Number - 1;
                            if (idx >= 0 && idx < com.Items.Count) com.SelectedIndex = idx;
                        }
                        else if (widget.InnerElement is TextBox textBox)
                        {
                            textBox.Text = value.String;
                        }
                        else if (widget.InnerElement is Image imgControl)
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
                        else if (widget.InnerElement is Canvas plotCanvas && widget.Type == "plot2d" && value.Type == DataType.Table)
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
                        else if (widget.InnerElement is Canvas plot3dCanvas && widget.Type == "plot3d" && value.Type == DataType.Table)
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
                            var tbl = value.Table;
                            var valX = tbl.Get("x");
                            var valY = tbl.Get("y");
                            var valZ = tbl.Get("z");

                            bool isStructured = valX.Type == DataType.Table || valY.Type == DataType.Table || valZ.Type == DataType.Table;

                            if (isStructured)
                            {
                                if (valX.Type == DataType.Table)
                                {
                                    widget.PlotXData = new List<double>();
                                    for (int i = 1; i <= valX.Table.Length; i++) widget.PlotXData.Add(valX.Table.Get(i).Number);
                                }
                                if (valY.Type == DataType.Table)
                                {
                                    widget.PlotYData = new List<double>();
                                    for (int i = 1; i <= valY.Table.Length; i++) widget.PlotYData.Add(valY.Table.Get(i).Number);
                                }
                                if (valZ.Type == DataType.Table)
                                {
                                    widget.PlotZData = new List<double>();
                                    for (int i = 1; i <= valZ.Table.Length; i++) widget.PlotZData.Add(valZ.Table.Get(i).Number);
                                }
                            }
                            else
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
                                        List<double> pts = new List<double>();
                                        for (int i = 1; i <= tbl.Length; i++) pts.Add(tbl.Get(i).Number);
                                        widget.Plot2DData = pts;
                                    }
                                    else if (parentWidget.Type == "plot3d")
                                    {
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
                                    }
                                }
                            }

                            GuiWidget? pWidget = null;
                            lock (_lock)
                            {
                                _widgets.TryGetValue(widget.ParentName, out pWidget);
                            }
                            if (pWidget != null && pWidget.InnerElement is Canvas pCanvas)
                            {
                                if (pWidget.Type == "plot2d") RenderPlot2D(pCanvas);
                                else if (pWidget.Type == "plot3d") RenderPlot3D(pCanvas);
                            }
                        }
                        else if (widget.InnerElement is ProgressBar pb)
                        {
                            pb.Value = value.Number;
                        }
                        else if (widget.InnerElement is Border colorBorder && widget.Type == "colorpicker")
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

        private static void ApplyNovaStyle(GuiWidget widget)
        {
            var element = widget.Element;
            var inner = widget.InnerElement;

            widget.OriginalBackground = ThemeManager.TitleBarBgBrush;

            if (inner is Control control)
            {
                control.FontFamily = new FontFamily("Inter, Segoe UI");
                control.FontSize = 11;
                control.Foreground = ThemeManager.TitleBarFgBrush;

                if (inner is Button btn)
                {
                    btn.Template = CreateSimpleButtonTemplate();
                }

                // If wrapped, the wrapper border handles background and border
                if (element is Border wrapper && wrapper != inner)
                {
                    wrapper.Background = ThemeManager.TitleBarBgBrush;
                    wrapper.BorderBrush = ThemeManager.BorderBrush;
                    wrapper.BorderThickness = new Thickness(1);

                    if (inner is Button || inner is TextBox || inner is ComboBox || inner is ProgressBar)
                    {
                        control.Background = Brushes.Transparent;
                        control.BorderBrush = Brushes.Transparent;
                        control.BorderThickness = new Thickness(0);
                    }
                }
                else
                {
                    control.Background = ThemeManager.TitleBarBgBrush;
                    control.BorderBrush = ThemeManager.BorderBrush;
                    control.BorderThickness = new Thickness(1);
                }
            }
            else if (inner is TextBlock tb)
            {
                tb.FontFamily = new FontFamily("Inter, Segoe UI");
                tb.FontSize = 11;
                tb.Foreground = ThemeManager.TitleBarFgBrush;
            }

            // If the outer element is a Border (like panel or colorpicker)
            if (element is Border border && border == inner)
            {
                border.Background = ThemeManager.TitleBarBgBrush;
                border.BorderBrush = ThemeManager.BorderBrush;
                border.BorderThickness = new Thickness(1);
            }
        }

        private static void HookWidgetEvents(GuiWidget widget)
        {
            var element = widget.Element;
            var inner = widget.InnerElement;

            element.MouseEnter += (s, e) =>
            {
                if (widget.HoverColor != null)
                {
                    if (element is Border border)
                    {
                        border.Background = widget.HoverColor;
                    }
                    else if (element is Panel panelControl)
                    {
                        panelControl.Background = widget.HoverColor;
                    }
                    else if (element is Control control)
                    {
                        control.Background = widget.HoverColor;
                    }
                }

                if (widget.OnHover != null && CurrentScript != null)
                {
                    PrintLog("INFO", $"[GUI] Hovered: {widget.Name}");
                    CurrentScript.Call(widget.OnHover, DynValue.NewString(widget.Name));
                }
            };

            element.MouseLeave += (s, e) =>
            {
                if (widget.HoverColor != null)
                {
                    var bg = widget.OriginalBackground ?? ThemeManager.TitleBarBgBrush;
                    if (element is Border border)
                    {
                        border.Background = bg;
                    }
                    else if (element is Panel panelControl)
                    {
                        panelControl.Background = bg;
                    }
                    else if (element is Control control)
                    {
                        control.Background = bg;
                    }
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
                    if (!(inner is Button) && !(inner is Border && widget.Type == "colorpicker"))
                    {
                        if (widget.OnClick != null && CurrentScript != null)
                        {
                            PrintLog("INFO", $"[GUI] Clicked: {widget.Name}");
                            CurrentScript.Call(widget.OnClick, DynValue.NewString(widget.Name));
                        }
                    }
                }
            };

            if (inner is Button btn)
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
            else if (inner is Slider slider)
            {
                slider.ValueChanged += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewNumber(slider.Value));
                    }
                };
            }
            else if (inner is CheckBox cb)
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
            else if (inner is RadioButton rb)
            {
                rb.Checked += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewBoolean(true));
                    }
                };
                rb.Unchecked += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewBoolean(false));
                    }
                };
            }
            else if (inner is ComboBox combo)
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
            else if (inner is TextBox tb)
            {
                tb.TextChanged += (s, e) =>
                {
                    if (widget.OnChanged != null && CurrentScript != null)
                    {
                        CurrentScript.Call(widget.OnChanged, DynValue.NewString(tb.Text));
                    }
                };
            }
            else if (inner is Border colorBorder && widget.Type == "colorpicker")
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
                        case "radiobutton": childWidth = 120; break;
                        case "textarea": childWidth = 150; break;
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

            var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            brush.Freeze();
            return brush;
        }

        public static void PrintLog(string level, string message)
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
            double width = canvas.Width;
            double height = canvas.Height;
            if (double.IsNaN(width) || width <= 0) width = 200;
            if (double.IsNaN(height) || height <= 0) height = 150;

            canvas.ClipToBounds = true;
            canvas.Clip = new RectangleGeometry(new Rect(0, 0, width, height));
            canvas.Children.Clear();

            if (!(canvas.Tag is Plot2DState plotState))
            {
                plotState = new Plot2DState();
                canvas.Tag = plotState;
            }

            // Determine topMargin
            double topMargin = 0;
            if (plotState.TitleVisible && !string.IsNullOrEmpty(plotState.Title))
            {
                topMargin = plotState.TitleFontSize + 12.0;
                var titleBorder = new Border
                {
                    Width = width,
                    Height = topMargin,
                    Background = plotState.TitleBackground ?? Brushes.Transparent,
                    Child = new TextBlock
                    {
                        Text = plotState.Title,
                        FontSize = plotState.TitleFontSize,
                        Foreground = plotState.TitleColor ?? ThemeManager.TitleBarFgBrush,
                        FontFamily = new FontFamily("Inter, Segoe UI"),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Canvas.SetLeft(titleBorder, 0);
                Canvas.SetTop(titleBorder, 0);
                canvas.Children.Add(titleBorder);
            }

            string plotName = string.Empty;
            GuiWidget? plotWidget = null;
            lock (_lock)
            {
                foreach (var pair in _widgets)
                {
                    if (pair.Value.Element == canvas || pair.Value.InnerElement == canvas)
                    {
                        plotName = pair.Key;
                        plotWidget = pair.Value;
                        break;
                    }
                }
            }

            double plotHeight = height - topMargin;
            if (plotHeight <= 0) plotHeight = 1;

            double borderRadius = plotWidget?.BorderRadius ?? 0.0;

            var bg = new Border
            {
                Width = width,
                Height = plotHeight,
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderBrush = ThemeManager.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(borderRadius)
            };
            Canvas.SetLeft(bg, 0);
            Canvas.SetTop(bg, topMargin);
            canvas.Children.Add(bg);

            var lines = GetChildPlotLines(plotName);
            var activeSeries = new List<SeriesPoints>();

            if (lines.Count > 0)
            {
                for (int idx = 0; idx < lines.Count; idx++)
                {
                    var lineWidget = lines[idx];
                    string seriesName = GetDisplayName(!string.IsNullOrEmpty(lineWidget.Legend) ? lineWidget.Legend : lineWidget.Name);
                    
                    // Skip if disabled
                    if (plotState.DisabledSeries.Contains(seriesName)) continue;

                    List<double> yData = lineWidget.PlotYData.Count > 0 ? lineWidget.PlotYData : lineWidget.Plot2DData;
                    List<double> xData = lineWidget.PlotXData.Count > 0 ? lineWidget.PlotXData : null!;
                    
                    if (yData.Count == 0) continue;
                    if (xData == null)
                    {
                        xData = new List<double>();
                        for (int i = 0; i < yData.Count; i++) xData.Add(i);
                    }

                    int count = Math.Min(xData.Count, yData.Count);
                    var sPoints = new SeriesPoints
                    {
                        Brush = lineWidget.CustomColor ?? SeriesColors[idx % SeriesColors.Length],
                        Type = lineWidget.PlotType,
                        Legend = seriesName,
                        Widget = lineWidget
                    };
                    for (int i = 0; i < count; i++)
                    {
                        sPoints.Points.Add(new Point(xData[i], yData[i]));
                    }
                    activeSeries.Add(sPoints);
                }
            }
            else
            {
                if (plotState.Data.Count > 0)
                {
                    string seriesName = GetDisplayName(plotState.Legend);
                    if (!plotState.DisabledSeries.Contains(seriesName))
                    {
                        var sPoints = new SeriesPoints
                        {
                            Brush = ThemeManager.AccentBrush,
                            Type = "line",
                            Legend = seriesName,
                            Widget = null!
                        };
                        for (int i = 0; i < plotState.Data.Count; i++)
                        {
                            sPoints.Points.Add(new Point(i, plotState.Data[i]));
                        }
                        activeSeries.Add(sPoints);
                    }
                }
            }

            if (activeSeries.Count == 0) return;

            // Find min/max bounds
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            bool hasPoints = false;

            foreach (var s in activeSeries)
            {
                foreach (var pt in s.Points)
                {
                    hasPoints = true;
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
            }

            if (!hasPoints)
            {
                minX = 0; maxX = 1;
                minY = 0; maxY = 1;
            }

            // Override with custom range configurations if provided
            if (plotState.RangeMinX.HasValue) minX = plotState.RangeMinX.Value;
            if (plotState.RangeMaxX.HasValue) maxX = plotState.RangeMaxX.Value;
            if (plotState.RangeMinY.HasValue) minY = plotState.RangeMinY.Value;
            if (plotState.RangeMaxY.HasValue) maxY = plotState.RangeMaxY.Value;

            double rangeX = maxX - minX;
            if (rangeX == 0) rangeX = 1;
            double rangeY = maxY - minY;
            if (rangeY == 0) rangeY = 1;

            // Draw grid lines based on range and tick spacing
            if (plotState.GridVisibleY)
            {
                if (plotState.TickIntervalY.HasValue && plotState.TickIntervalY.Value > 0)
                {
                    double interval = plotState.TickIntervalY.Value;
                    double start = Math.Ceiling(minY / interval) * interval;
                    for (double val = start; val <= maxY; val += interval)
                    {
                        if (val == minY || val == maxY) continue;
                        double sy = topMargin + plotHeight - (plotHeight * (val - minY) / rangeY);
                        var gridLine = new Line
                        {
                            X1 = 0,
                            Y1 = sy,
                            X2 = width,
                            Y2 = sy,
                            Stroke = plotState.GridColorY ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = plotState.GridThicknessY
                        };
                        canvas.Children.Add(gridLine);
                    }
                }
                else if (plotState.GridIntervalY > 0)
                {
                    int countY = (int)Math.Max(1, plotState.GridIntervalY);
                    for (int i = 1; i < countY; i++)
                    {
                        double y = topMargin + plotHeight * i / (double)countY;
                        var gridLine = new Line
                        {
                            X1 = 0,
                            Y1 = y,
                            X2 = width,
                            Y2 = y,
                            Stroke = plotState.GridColorY ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = plotState.GridThicknessY
                        };
                        canvas.Children.Add(gridLine);
                    }
                }
            }

            if (plotState.GridVisibleX)
            {
                if (plotState.TickIntervalX.HasValue && plotState.TickIntervalX.Value > 0)
                {
                    double interval = plotState.TickIntervalX.Value;
                    double start = Math.Ceiling(minX / interval) * interval;
                    for (double val = start; val <= maxX; val += interval)
                    {
                        if (val == minX || val == maxX) continue;
                        double sx = width * (val - minX) / rangeX;
                        var gridLine = new Line
                        {
                            X1 = sx,
                            Y1 = topMargin,
                            X2 = sx,
                            Y2 = topMargin + plotHeight,
                            Stroke = plotState.GridColorX ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = plotState.GridThicknessX
                        };
                        canvas.Children.Add(gridLine);
                    }
                }
                else if (plotState.GridIntervalX > 0)
                {
                    int countX = (int)Math.Max(1, plotState.GridIntervalX);
                    for (int i = 1; i < countX; i++)
                    {
                        double x = width * i / (double)countX;
                        var gridLine = new Line
                        {
                            X1 = x,
                            Y1 = topMargin,
                            X2 = x,
                            Y2 = topMargin + plotHeight,
                            Stroke = plotState.GridColorX ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = plotState.GridThicknessX
                        };
                        canvas.Children.Add(gridLine);
                    }
                }
            }

            // Render series
            foreach (var s in activeSeries)
            {
                var brush = s.Brush;
                var type = s.Type;
                var widget = s.Widget;

                if (type == "scatter")
                {
                    double markerSize = widget != null ? widget.MarkerSize : 6.0;
                    string markerStyle = widget != null ? widget.MarkerStyle : "circle";
                    Brush markerColor = (widget != null && widget.MarkerColor != null) ? widget.MarkerColor : brush;
                    double r = markerSize / 2.0;

                    for (int i = 0; i < s.Points.Count; i++)
                     {
                        double sx = width * (s.Points[i].X - minX) / rangeX;
                        double sy = topMargin + plotHeight - (plotHeight * (s.Points[i].Y - minY) / rangeY);

                        if (markerStyle == "square")
                        {
                            var sq = new Rectangle
                            {
                                Width = markerSize,
                                Height = markerSize,
                                Fill = markerColor,
                                Margin = new Thickness(sx - r, sy - r, 0, 0)
                            };
                            canvas.Children.Add(sq);
                        }
                        else if (markerStyle == "triangle")
                        {
                            var tri = new Polygon
                            {
                                Points = new PointCollection
                                {
                                    new Point(sx, sy - r),
                                    new Point(sx - r, sy + r),
                                    new Point(sx + r, sy + r)
                                },
                                Fill = markerColor
                            };
                            canvas.Children.Add(tri);
                        }
                        else
                        {
                            var dot = new Ellipse
                            {
                                Width = markerSize,
                                Height = markerSize,
                                Fill = markerColor,
                                Margin = new Thickness(sx - r, sy - r, 0, 0)
                            };
                            canvas.Children.Add(dot);
                        }
                    }
                }
                else if (type == "bar")
                {
                    double barWidth = (widget != null && widget.BarWidthValue > 0)
                        ? widget.BarWidthValue
                        : (width / Math.Max(1, s.Points.Count)) * 0.6;

                    Brush fillBrush = (widget != null && widget.BarColor != null) ? widget.BarColor : brush;
                    if (widget != null && widget.BarStyle == "gradient" && fillBrush is SolidColorBrush solidBrush)
                    {
                        var startColor = solidBrush.Color;
                        var endColor = Color.FromArgb(20, startColor.R, startColor.G, startColor.B);
                        var gradBrush = new LinearGradientBrush(startColor, endColor, new Point(0.5, 0), new Point(0.5, 1));
                        gradBrush.Freeze();
                        fillBrush = gradBrush;
                    }

                    for (int i = 0; i < s.Points.Count; i++)
                    {
                        double sx = width * (s.Points[i].X - minX) / rangeX;
                        double sy = topMargin + plotHeight - (plotHeight * (s.Points[i].Y - minY) / rangeY);

                        double x = sx - barWidth / 2.0;
                        double valY = sy;
                        double barHeight = (topMargin + plotHeight) - valY;
                        if (barHeight <= 0) barHeight = 1;

                        var rect = new Rectangle
                        {
                            Width = barWidth,
                            Height = barHeight,
                            Fill = fillBrush,
                            Opacity = 0.85
                        };
                        Canvas.SetLeft(rect, x);
                        Canvas.SetTop(rect, valY);
                        canvas.Children.Add(rect);
                    }
                }
                else
                {
                    // line
                    double thickness = widget != null ? widget.LineThickness : 2.0;
                    string lineStyle = widget != null ? widget.LineStyle : "solid";

                    DoubleCollection? dashArray = null;
                    if (lineStyle == "dashed") dashArray = new DoubleCollection { 4, 4 };
                    else if (lineStyle == "dotted") dashArray = new DoubleCollection { 1, 3 };

                    var polyline = new Polyline
                    {
                        Stroke = brush,
                        StrokeThickness = thickness,
                        StrokeDashArray = dashArray
                    };

                    for (int i = 0; i < s.Points.Count; i++)
                    {
                        double sx = width * (s.Points[i].X - minX) / rangeX;
                        double sy = topMargin + plotHeight - (plotHeight * (s.Points[i].Y - minY) / rangeY);
                        polyline.Points.Add(new Point(sx, sy));
                    }

                    canvas.Children.Add(polyline);
                }
            }

            // Render Legends
            List<string> allLegends = new List<string>();
            List<Brush> legendColors = new List<Brush>();
            if (lines.Count > 0)
            {
                for (int idx = 0; idx < lines.Count; idx++)
                {
                    var lineWidget = lines[idx];
                    allLegends.Add(GetDisplayName(!string.IsNullOrEmpty(lineWidget.Legend) ? lineWidget.Legend : lineWidget.Name));
                    legendColors.Add(lineWidget.CustomColor ?? SeriesColors[idx % SeriesColors.Length]);
                }
            }
            else
            {
                if (plotState.Data.Count > 0)
                {
                    allLegends.Add(GetDisplayName(plotState.Legend));
                    legendColors.Add(ThemeManager.AccentBrush);
                }
            }

            bool hasAnyLegend = allLegends.Any(l => !string.IsNullOrEmpty(l));
            if (hasAnyLegend)
            {
                var legendPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };

                for (int d = 0; d < allLegends.Count; d++)
                {
                    string legText = allLegends[d];
                    if (string.IsNullOrEmpty(legText)) continue;

                    bool isDisabled = plotState.DisabledSeries.Contains(legText);
                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };

                    var accentLine = new Line
                    {
                        X1 = 0,
                        Y1 = 0,
                        X2 = 12,
                        Y2 = 0,
                        Stroke = legendColors[d],
                        StrokeThickness = 2,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0),
                        Opacity = isDisabled ? 0.25 : 1.0
                    };
                    itemPanel.Children.Add(accentLine);

                    Brush textBrush = plotState.LegendTextColor ?? ThemeManager.TitleBarFgBrush;
                    if (isDisabled)
                    {
                        var mutedBrush = new SolidColorBrush(Color.FromArgb(80, 205, 214, 244));
                        mutedBrush.Freeze();
                        textBrush = mutedBrush;
                    }

                    var legendLabel = new TextBlock
                    {
                        Text = legText,
                        Foreground = textBrush,
                        FontSize = 9,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    itemPanel.Children.Add(legendLabel);

                    itemPanel.Cursor = Cursors.Hand;
                    itemPanel.Background = Brushes.Transparent;
                    string seriesName = legText;
                    itemPanel.MouseDown += (s, e) =>
                    {
                        if (plotState.DisabledSeries.Contains(seriesName))
                        {
                            plotState.DisabledSeries.Remove(seriesName);
                        }
                        else
                        {
                            plotState.DisabledSeries.Add(seriesName);
                        }
                        RenderPlot2D(canvas);
                    };

                    legendPanel.Children.Add(itemPanel);
                }

                var legendBorder = new Border
                {
                    Background = plotState.LegendBgColor ?? new SolidColorBrush(Color.FromArgb(200, 17, 17, 27)),
                    BorderBrush = ThemeManager.BorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 4, 6, 4),
                    Child = legendPanel
                };

                Canvas.SetTop(legendBorder, topMargin + 6);
                Canvas.SetRight(legendBorder, 6);
                canvas.Children.Add(legendBorder);
            }
        }

        private static void RenderPlot3D(Canvas canvas)
        {
            double width = canvas.Width;
            double height = canvas.Height;
            if (double.IsNaN(width) || width <= 0) width = 200;
            if (double.IsNaN(height) || height <= 0) height = 150;

            canvas.ClipToBounds = true;
            canvas.Clip = new RectangleGeometry(new Rect(0, 0, width, height));
            canvas.Children.Clear();

            Plot3DState? state = canvas.Tag as Plot3DState;
            if (state == null)
            {
                state = new Plot3DState();
                canvas.Tag = state;
            }

            if (!state.EventsRegistered)
            {
                state.EventsRegistered = true;
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

            // Determine topMargin
            double topMargin = 0;
            if (state.TitleVisible && !string.IsNullOrEmpty(state.Title))
            {
                topMargin = state.TitleFontSize + 12.0;
                var titleBorder = new Border
                {
                    Width = width,
                    Height = topMargin,
                    Background = state.TitleBackground ?? Brushes.Transparent,
                    Child = new TextBlock
                    {
                        Text = state.Title,
                        FontSize = state.TitleFontSize,
                        Foreground = state.TitleColor ?? ThemeManager.TitleBarFgBrush,
                        FontFamily = new FontFamily("Inter, Segoe UI"),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Canvas.SetLeft(titleBorder, 0);
                Canvas.SetTop(titleBorder, 0);
                canvas.Children.Add(titleBorder);
            }

            string plotName = string.Empty;
            GuiWidget? plotWidget = null;
            lock (_lock)
            {
                foreach (var pair in _widgets)
                {
                    if (pair.Value.Element == canvas || pair.Value.InnerElement == canvas)
                    {
                        plotName = pair.Key;
                        plotWidget = pair.Value;
                        break;
                    }
                }
            }

            double plotHeight = height - topMargin;
            if (plotHeight <= 0) plotHeight = 1;

            double borderRadius = plotWidget?.BorderRadius ?? 0.0;

            var bg = new Border
            {
                Width = width,
                Height = plotHeight,
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                BorderBrush = ThemeManager.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(borderRadius)
            };
            Canvas.SetLeft(bg, 0);
            Canvas.SetTop(bg, topMargin);
            canvas.Children.Add(bg);

            var lines = GetChildPlotLines(plotName);
            var activeSeries3D = new List<SeriesPoints3D>();

            if (lines.Count > 0)
            {
                for (int idx = 0; idx < lines.Count; idx++)
                {
                    var lineWidget = lines[idx];
                    string seriesName = GetDisplayName(!string.IsNullOrEmpty(lineWidget.Legend) ? lineWidget.Legend : lineWidget.Name);
                    if (state.DisabledSeries.Contains(seriesName)) continue;

                    var s3d = new SeriesPoints3D
                    {
                        Brush = lineWidget.CustomColor ?? SeriesColors[idx % SeriesColors.Length],
                        Type = lineWidget.PlotType,
                        Legend = seriesName,
                        Widget = lineWidget
                    };

                    var xData = lineWidget.PlotXData;
                    var yData = lineWidget.PlotYData;
                    var zData = lineWidget.PlotZData;

                    if (xData.Count > 0 || yData.Count > 0 || zData.Count > 0)
                    {
                        int count = Math.Max(xData.Count, Math.Max(yData.Count, zData.Count));
                        for (int i = 0; i < count; i++)
                        {
                            double x = i < xData.Count ? xData[i] : (i < yData.Count ? yData[i] : 0);
                            double y = i < yData.Count ? yData[i] : (i < xData.Count ? xData[i] : 0);
                            double z = i < zData.Count ? zData[i] : 0;
                            s3d.Points.Add(new Point3D(x, y, z));
                        }
                    }
                    else if (lineWidget.Plot3DData.Count > 0 && lineWidget.Plot3DData[0].Count > 0)
                    {
                        s3d.GridData = lineWidget.Plot3DData;
                    }

                    if (s3d.Points.Count > 0 || s3d.GridData != null)
                    {
                        activeSeries3D.Add(s3d);
                    }
                }
            }
            else
            {
                if (state.GridData.Count > 0 && state.GridData[0].Count > 0)
                {
                    string seriesName = GetDisplayName(state.Legend);
                    if (!state.DisabledSeries.Contains(seriesName))
                    {
                        var s3d = new SeriesPoints3D
                        {
                            Brush = ThemeManager.AccentBrush,
                            Type = "line",
                            Legend = seriesName,
                            GridData = state.GridData,
                            Widget = null!
                        };
                        activeSeries3D.Add(s3d);
                    }
                }
            }

            if (activeSeries3D.Count == 0) return;

            // Global limits
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            double minZ = double.MaxValue;
            double maxZ = double.MinValue;
            bool hasData = false;

            foreach (var s in activeSeries3D)
            {
                if (s.Points.Count > 0)
                {
                    hasData = true;
                    foreach (var pt in s.Points)
                    {
                        if (pt.X < minX) minX = pt.X;
                        if (pt.X > maxX) maxX = pt.X;
                        if (pt.Y < minY) minY = pt.Y;
                        if (pt.Y > maxY) maxY = pt.Y;
                        if (pt.Z < minZ) minZ = pt.Z;
                        if (pt.Z > maxZ) maxZ = pt.Z;
                    }
                }
                else if (s.GridData != null)
                {
                    hasData = true;
                    int rows = s.GridData.Count;
                    int cols = s.GridData[0].Count;

                    if (0 < minX) minX = 0;
                    if (cols - 1 > maxX) maxX = cols - 1;
                    if (0 < minY) minY = 0;
                    if (rows - 1 > maxY) maxY = rows - 1;

                    foreach (var row in s.GridData)
                    {
                        foreach (var val in row)
                        {
                            if (val < minZ) minZ = val;
                            if (val > maxZ) maxZ = val;
                        }
                    }
                }
            }

            if (!hasData)
            {
                minX = 0; maxX = 1;
                minY = 0; maxY = 1;
                minZ = 0; maxZ = 1;
            }

            // Override with custom range configurations if provided
            if (state.RangeMinX.HasValue) minX = state.RangeMinX.Value;
            if (state.RangeMaxX.HasValue) maxX = state.RangeMaxX.Value;
            if (state.RangeMinY.HasValue) minY = state.RangeMinY.Value;
            if (state.RangeMaxY.HasValue) maxY = state.RangeMaxY.Value;
            if (state.RangeMinZ.HasValue) minZ = state.RangeMinZ.Value;
            if (state.RangeMaxZ.HasValue) maxZ = state.RangeMaxZ.Value;

            double rangeX = maxX - minX; if (rangeX == 0) rangeX = 1;
            double rangeY = maxY - minY; if (rangeY == 0) rangeY = 1;
            double rangeZ = maxZ - minZ; if (rangeZ == 0) rangeZ = 1;

            double radX = state.RotateX * Math.PI / 180.0;
            double radY = state.RotateY * Math.PI / 180.0;
            double cosX = Math.Cos(radX);
            double sinX = Math.Sin(radX);
            double cosY = Math.Cos(radY);
            double sinY = Math.Sin(radY);

            // Create a nested canvas container specifically for the 3D plot graph elements
            // which clips all graph items precisely to the visual plot area (excluding title)
            var graphContainer = new Canvas
            {
                Width = width,
                Height = plotHeight,
                ClipToBounds = true,
                Clip = new RectangleGeometry(new Rect(0, 0, width, plotHeight))
            };
            Canvas.SetLeft(graphContainer, 0);
            Canvas.SetTop(graphContainer, topMargin);
            canvas.Children.Add(graphContainer);

            Point ProjectCoord(double xVal, double yVal, double zVal)
            {
                double x = (xVal - minX) / rangeX - 0.5;
                double y = (yVal - minY) / rangeY - 0.5;
                double z = (zVal - minZ) / rangeZ - 0.5;

                double y1 = y * cosY - z * sinY;
                double z1 = y * sinY + z * cosY;

                double x2 = x * cosX - y1 * sinX;
                double y2 = x * sinX + y1 * cosX;

                double scale = width * 0.7 * state.Zoom;
                double screenX = width / 2.0 + x2 * scale;
                double centerY = plotHeight / 2.0; // center relative to graphContainer
                double screenY = centerY + y2 * scale - z1 * (scale * 0.5);

                return new Point(screenX, screenY);
            }

            // Draw X grid lines
            if (state.GridVisibleX)
            {
                if (state.TickIntervalX.HasValue && state.TickIntervalX.Value > 0)
                {
                    double interval = state.TickIntervalX.Value;
                    double start = Math.Ceiling(minX / interval) * interval;
                    for (double val = start; val <= maxX; val += interval)
                    {
                        var pt1 = ProjectCoord(val, minY, minZ);
                        var pt2 = ProjectCoord(val, maxY, minZ);
                        var gridLine = new Line
                        {
                            X1 = pt1.X, Y1 = pt1.Y,
                            X2 = pt2.X, Y2 = pt2.Y,
                            Stroke = state.GridColorX ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = state.GridThicknessX
                        };
                        graphContainer.Children.Add(gridLine);
                    }
                }
                else if (state.GridIntervalX > 0)
                {
                    int countX = (int)Math.Max(1, state.GridIntervalX);
                    for (int i = 0; i <= countX; i++)
                    {
                        double xVal = minX + rangeX * i / (double)countX;
                        var pt1 = ProjectCoord(xVal, minY, minZ);
                        var pt2 = ProjectCoord(xVal, maxY, minZ);
                        var gridLine = new Line
                        {
                            X1 = pt1.X, Y1 = pt1.Y,
                            X2 = pt2.X, Y2 = pt2.Y,
                            Stroke = state.GridColorX ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = state.GridThicknessX
                        };
                        graphContainer.Children.Add(gridLine);
                    }
                }
            }

            // Draw Y grid lines
            if (state.GridVisibleY)
            {
                if (state.TickIntervalY.HasValue && state.TickIntervalY.Value > 0)
                {
                    double interval = state.TickIntervalY.Value;
                    double start = Math.Ceiling(minY / interval) * interval;
                    for (double val = start; val <= maxY; val += interval)
                    {
                        var pt1 = ProjectCoord(minX, val, minZ);
                        var pt2 = ProjectCoord(maxX, val, minZ);
                        var gridLine = new Line
                        {
                            X1 = pt1.X, Y1 = pt1.Y,
                            X2 = pt2.X, Y2 = pt2.Y,
                            Stroke = state.GridColorY ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = state.GridThicknessY
                        };
                        graphContainer.Children.Add(gridLine);
                    }
                }
                else if (state.GridIntervalY > 0)
                {
                    int countY = (int)Math.Max(1, state.GridIntervalY);
                    for (int i = 0; i <= countY; i++)
                    {
                        double yVal = minY + rangeY * i / (double)countY;
                        var pt1 = ProjectCoord(minX, yVal, minZ);
                        var pt2 = ProjectCoord(maxX, yVal, minZ);
                        var gridLine = new Line
                        {
                            X1 = pt1.X, Y1 = pt1.Y,
                            X2 = pt2.X, Y2 = pt2.Y,
                            Stroke = state.GridColorY ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                            StrokeThickness = state.GridThicknessY
                        };
                        graphContainer.Children.Add(gridLine);
                    }
                }
            }

            // Draw Z grid cage lines
            if (state.GridVisibleZ)
            {
                var corners = new[]
                {
                    new Point(minX, minY),
                    new Point(maxX, minY),
                    new Point(minX, maxY),
                    new Point(maxX, maxY)
                };

                foreach (var corner in corners)
                {
                    var pt1 = ProjectCoord(corner.X, corner.Y, minZ);
                    var pt2 = ProjectCoord(corner.X, corner.Y, maxZ);
                    var gridLine = new Line
                    {
                        X1 = pt1.X, Y1 = pt1.Y,
                        X2 = pt2.X, Y2 = pt2.Y,
                        Stroke = state.GridColorZ ?? new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                        StrokeThickness = state.GridThicknessZ
                    };
                    graphContainer.Children.Add(gridLine);
                }

                var cageColor = state.GridColorZ ?? new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));

                if (state.TickIntervalZ.HasValue && state.TickIntervalZ.Value > 0)
                {
                    double interval = state.TickIntervalZ.Value;
                    double start = Math.Ceiling(minZ / interval) * interval;
                    for (double val = start; val <= maxZ; val += interval)
                    {
                        var p1 = ProjectCoord(minX, minY, val);
                        var p2 = ProjectCoord(maxX, minY, val);
                        var p3 = ProjectCoord(maxX, maxY, val);
                        var p4 = ProjectCoord(minX, maxY, val);

                        graphContainer.Children.Add(new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                        graphContainer.Children.Add(new Line { X1 = p2.X, Y1 = p2.Y, X2 = p3.X, Y2 = p3.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                        graphContainer.Children.Add(new Line { X1 = p3.X, Y1 = p3.Y, X2 = p4.X, Y2 = p4.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                        graphContainer.Children.Add(new Line { X1 = p4.X, Y1 = p4.Y, X2 = p1.X, Y2 = p1.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                    }
                }
                else if (state.GridIntervalZ > 0)
                {
                    int countZ = (int)Math.Max(1, state.GridIntervalZ);
                    for (int i = 1; i <= countZ; i++)
                    {
                        double zVal = minZ + rangeZ * i / (double)countZ;
                        var p1 = ProjectCoord(minX, minY, zVal);
                        var p2 = ProjectCoord(maxX, minY, zVal);
                        var p3 = ProjectCoord(maxX, maxY, zVal);
                        var p4 = ProjectCoord(minX, maxY, zVal);

                        graphContainer.Children.Add(new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                        graphContainer.Children.Add(new Line { X1 = p2.X, Y1 = p2.Y, X2 = p3.X, Y2 = p3.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                        graphContainer.Children.Add(new Line { X1 = p3.X, Y1 = p3.Y, X2 = p4.X, Y2 = p4.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                        graphContainer.Children.Add(new Line { X1 = p4.X, Y1 = p4.Y, X2 = p1.X, Y2 = p1.Y, Stroke = cageColor, StrokeThickness = state.GridThicknessZ });
                    }
                }
            }

            // Render series
            foreach (var s in activeSeries3D)
            {
                if (s.Points.Count > 0)
                {
                    if (s.Type == "scatter")
                    {
                        double markerSize = s.Widget != null ? s.Widget.MarkerSize : 6.0;
                        string markerStyle = s.Widget != null ? s.Widget.MarkerStyle : "circle";
                        Brush markerColor = (s.Widget != null && s.Widget.MarkerColor != null) ? s.Widget.MarkerColor : s.Brush;
                        double r = markerSize / 2.0;

                        for (int i = 0; i < s.Points.Count; i++)
                        {
                            var pt = ProjectCoord(s.Points[i].X, s.Points[i].Y, s.Points[i].Z);
                            if (markerStyle == "square")
                            {
                                var sq = new Rectangle
                                {
                                    Width = markerSize, Height = markerSize,
                                    Fill = markerColor,
                                    Margin = new Thickness(pt.X - r, pt.Y - r, 0, 0)
                                };
                                graphContainer.Children.Add(sq);
                            }
                            else if (markerStyle == "triangle")
                            {
                                var tri = new Polygon
                                {
                                    Points = new PointCollection
                                    {
                                        new Point(pt.X, pt.Y - r),
                                        new Point(pt.X - r, pt.Y + r),
                                        new Point(pt.X + r, pt.Y + r)
                                    },
                                    Fill = markerColor
                                };
                                graphContainer.Children.Add(tri);
                            }
                            else
                            {
                                var dot = new Ellipse
                                {
                                    Width = markerSize, Height = markerSize,
                                    Fill = markerColor,
                                    Margin = new Thickness(pt.X - r, pt.Y - r, 0, 0)
                                };
                                graphContainer.Children.Add(dot);
                            }
                        }
                    }
                    else
                    {
                        // line
                        double thickness = s.Widget != null ? s.Widget.LineThickness : 1.0;
                        string lineStyle = s.Widget != null ? s.Widget.LineStyle : "solid";

                        DoubleCollection? dashArray = null;
                        if (lineStyle == "dashed") dashArray = new DoubleCollection { 4, 4 };
                        else if (lineStyle == "dotted") dashArray = new DoubleCollection { 1, 3 };

                        for (int i = 0; i < s.Points.Count - 1; i++)
                        {
                            var pt1 = ProjectCoord(s.Points[i].X, s.Points[i].Y, s.Points[i].Z);
                            var pt2 = ProjectCoord(s.Points[i+1].X, s.Points[i+1].Y, s.Points[i+1].Z);

                            var line = new Line
                            {
                                X1 = pt1.X, Y1 = pt1.Y,
                                X2 = pt2.X, Y2 = pt2.Y,
                                Stroke = s.Brush,
                                StrokeThickness = thickness,
                                StrokeDashArray = dashArray
                            };
                            graphContainer.Children.Add(line);
                        }
                    }
                }
                else if (s.GridData != null)
                {
                    int rows = s.GridData.Count;
                    int cols = s.GridData[0].Count;

                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols - 1; c++)
                        {
                            var pt1 = ProjectCoord(c, r, s.GridData[r][c]);
                            var pt2 = ProjectCoord(c + 1, r, s.GridData[r][c + 1]);
                            var line = new Line
                            {
                                X1 = pt1.X, Y1 = pt1.Y,
                                X2 = pt2.X, Y2 = pt2.Y,
                                Stroke = s.Brush,
                                StrokeThickness = 1
                            };
                            graphContainer.Children.Add(line);
                        }
                    }

                    for (int c = 0; c < cols; c++)
                    {
                        for (int r = 0; r < rows - 1; r++)
                        {
                            var pt1 = ProjectCoord(c, r, s.GridData[r][c]);
                            var pt2 = ProjectCoord(c, r + 1, s.GridData[r + 1][c]);
                            var line = new Line
                            {
                                X1 = pt1.X, Y1 = pt1.Y,
                                X2 = pt2.X, Y2 = pt2.Y,
                                Stroke = s.Brush,
                                StrokeThickness = 1
                            };
                            graphContainer.Children.Add(line);
                        }
                    }
                }
            }

            // Render Legends
            List<string> allLegends = new List<string>();
            List<Brush> legendColors = new List<Brush>();
            if (lines.Count > 0)
            {
                for (int idx = 0; idx < lines.Count; idx++)
                {
                    var lineWidget = lines[idx];
                    allLegends.Add(GetDisplayName(!string.IsNullOrEmpty(lineWidget.Legend) ? lineWidget.Legend : lineWidget.Name));
                    legendColors.Add(lineWidget.CustomColor ?? SeriesColors[idx % SeriesColors.Length]);
                }
            }
            else
            {
                if (state.GridData.Count > 0 && state.GridData[0].Count > 0)
                {
                    allLegends.Add(GetDisplayName(state.Legend));
                    legendColors.Add(ThemeManager.AccentBrush);
                }
            }

            bool hasAnyLegend = allLegends.Any(l => !string.IsNullOrEmpty(l));
            if (hasAnyLegend)
            {
                var legendPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0) };

                for (int d = 0; d < allLegends.Count; d++)
                {
                    string legText = allLegends[d];
                    if (string.IsNullOrEmpty(legText)) continue;

                    bool isDisabled = state.DisabledSeries.Contains(legText);
                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };

                    var accentMarker = new Rectangle
                    {
                        Width = 8,
                        Height = 8,
                        Fill = legendColors[d],
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0),
                        Opacity = isDisabled ? 0.25 : 1.0
                    };
                    itemPanel.Children.Add(accentMarker);

                    Brush textBrush = state.LegendTextColor ?? ThemeManager.TitleBarFgBrush;
                    if (isDisabled)
                    {
                        var mutedBrush = new SolidColorBrush(Color.FromArgb(80, 205, 214, 244));
                        mutedBrush.Freeze();
                        textBrush = mutedBrush;
                    }

                    var legendLabel = new TextBlock
                    {
                        Text = legText,
                        Foreground = textBrush,
                        FontSize = 9,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    itemPanel.Children.Add(legendLabel);

                    itemPanel.Cursor = Cursors.Hand;
                    itemPanel.Background = Brushes.Transparent;
                    string seriesName = legText;
                    itemPanel.MouseDown += (s, e) =>
                    {
                        if (state.DisabledSeries.Contains(seriesName))
                        {
                            state.DisabledSeries.Remove(seriesName);
                        }
                        else
                        {
                            state.DisabledSeries.Add(seriesName);
                        }
                        RenderPlot3D(canvas);
                    };

                    legendPanel.Children.Add(itemPanel);
                }

                var legendBorder = new Border
                {
                    Background = state.LegendBgColor ?? new SolidColorBrush(Color.FromArgb(200, 17, 17, 27)),
                    BorderBrush = ThemeManager.BorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 4, 6, 4),
                    Child = legendPanel
                };

                Canvas.SetTop(legendBorder, topMargin + 6);
                Canvas.SetRight(legendBorder, 6);
                canvas.Children.Add(legendBorder);
            }

            // Create view-snapping buttons overlay at the bottom-left of the plot3d canvas
            var snapsIsVertical = state.SnapsOrientation.ToLower() == "vertical";
            var snapPanel = new StackPanel
            {
                Orientation = snapsIsVertical ? Orientation.Vertical : Orientation.Horizontal,
                Margin = new Thickness(0)
            };
            string[] views = { "Top", "Bottom", "Front", "Back", "Left", "Right" };
            double[] rotXs = { 0, 0, 0, 180, 90, -90 };
            double[] rotYs = { 90, -90, 0, 0, 0, 0 };

            for (int v = 0; v < views.Length; v++)
            {
                string viewName = views[v];
                double targetX = rotXs[v];
                double targetY = rotYs[v];

                var btnBorder = new Border
                {
                    Background = state.SnapsBgColor ?? new SolidColorBrush(Color.FromArgb(200, 30, 30, 46)),
                    BorderBrush = ThemeManager.BorderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = snapsIsVertical ? new Thickness(0, 2, 0, 2) : new Thickness(2, 0, 2, 0),
                    Cursor = Cursors.Hand
                };

                var btnText = new TextBlock
                {
                    Text = viewName,
                    FontSize = 9,
                    Foreground = state.SnapsTextColor ?? ThemeManager.TitleBarFgBrush,
                    FontFamily = new FontFamily("Inter, Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                btnBorder.Child = btnText;

                btnBorder.MouseEnter += (s, e) =>
                {
                    btnBorder.Background = ThemeManager.AccentBrush;
                    btnText.Foreground = new SolidColorBrush(Color.FromRgb(17, 17, 27));
                };
                btnBorder.MouseLeave += (s, e) =>
                {
                    btnBorder.Background = state.SnapsBgColor ?? new SolidColorBrush(Color.FromArgb(200, 30, 30, 46));
                    btnText.Foreground = state.SnapsTextColor ?? ThemeManager.TitleBarFgBrush;
                };

                btnBorder.MouseDown += (s, e) =>
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        state.RotateX = targetX;
                        state.RotateY = targetY;
                        RenderPlot3D(canvas);
                        e.Handled = true;
                    }
                };

                snapPanel.Children.Add(btnBorder);
            }

            var snapBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 17, 17, 27)),
                BorderBrush = ThemeManager.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4),
                Child = snapPanel
            };

            Canvas.SetBottom(snapBorder, 6);
            Canvas.SetLeft(snapBorder, 6);
            canvas.Children.Add(snapBorder);
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
        public Brush? LegendTextColor { get; set; }
        public Brush? LegendBgColor { get; set; }
        public HashSet<string> DisabledSeries { get; } = new HashSet<string>();

        // Custom Title properties
        public string Title { get; set; } = string.Empty;
        public double TitleFontSize { get; set; } = 12.0;
        public Brush? TitleColor { get; set; }
        public Brush? TitleBackground { get; set; }
        public bool TitleVisible { get; set; } = true;

        // Custom Grid properties per axis
        public bool GridVisibleX { get; set; } = true;
        public bool GridVisibleY { get; set; } = true;
        public double GridIntervalX { get; set; } = 4.0;
        public double GridIntervalY { get; set; } = 4.0;
        public Brush? GridColorX { get; set; }
        public Brush? GridColorY { get; set; }
        public double GridThicknessX { get; set; } = 0.5;
        public double GridThicknessY { get; set; } = 0.5;

        // Custom Range & Tick properties
        public double? RangeMinX { get; set; }
        public double? RangeMaxX { get; set; }
        public double? RangeMinY { get; set; }
        public double? RangeMaxY { get; set; }
        public double? TickIntervalX { get; set; }
        public double? TickIntervalY { get; set; }
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
        public Brush? LegendTextColor { get; set; }
        public Brush? LegendBgColor { get; set; }
        public HashSet<string> DisabledSeries { get; } = new HashSet<string>();

        // Restores dragging/zoom events register check
        public bool EventsRegistered { get; set; } = false;

        // Custom Title properties
        public string Title { get; set; } = string.Empty;
        public double TitleFontSize { get; set; } = 12.0;
        public Brush? TitleColor { get; set; }
        public Brush? TitleBackground { get; set; }
        public bool TitleVisible { get; set; } = true;

        // Custom Grid properties per axis
        public bool GridVisibleX { get; set; } = true;
        public bool GridVisibleY { get; set; } = true;
        public bool GridVisibleZ { get; set; } = true;
        public double GridIntervalX { get; set; } = 4.0;
        public double GridIntervalY { get; set; } = 4.0;
        public double GridIntervalZ { get; set; } = 4.0;
        public Brush? GridColorX { get; set; }
        public Brush? GridColorY { get; set; }
        public Brush? GridColorZ { get; set; }
        public double GridThicknessX { get; set; } = 0.5;
        public double GridThicknessY { get; set; } = 0.5;
        public double GridThicknessZ { get; set; } = 0.5;

        // Custom Range & Tick properties
        public double? RangeMinX { get; set; }
        public double? RangeMaxX { get; set; }
        public double? RangeMinY { get; set; }
        public double? RangeMaxY { get; set; }
        public double? RangeMinZ { get; set; }
        public double? RangeMaxZ { get; set; }
        public double? TickIntervalX { get; set; }
        public double? TickIntervalY { get; set; }
        public double? TickIntervalZ { get; set; }

        // Snap buttons styling properties
        public string SnapsOrientation { get; set; } = "horizontal";
        public Brush? SnapsTextColor { get; set; }
        public Brush? SnapsBgColor { get; set; }
    }

    public class SeriesPoints
    {
        public List<Point> Points { get; set; } = new List<Point>();
        public Brush Brush { get; set; } = null!;
        public string Type { get; set; } = "line";
        public string Legend { get; set; } = string.Empty;
        public GuiWidget Widget { get; set; } = null!;
    }

    public struct Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    public class SeriesPoints3D
    {
        public List<Point3D> Points { get; set; } = new List<Point3D>();
        public List<List<double>> GridData { get; set; } = null!;
        public Brush Brush { get; set; } = null!;
        public string Type { get; set; } = "line";
        public string Legend { get; set; } = string.Empty;
        public GuiWidget Widget { get; set; } = null!;
    }
}

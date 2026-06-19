using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using FlowEngine.Engine;

namespace FlowEngine
{
    public partial class MainWindow : Window
    {
        private string _nodesDirectory = string.Empty;
        private System.Diagnostics.PerformanceCounter? _cpuCounter;
        private DateTime _lastCpuTime = DateTime.UtcNow;
        private TimeSpan _lastCpuUsage = TimeSpan.Zero;
        private double _lastCpuUsagePercent = 0;
        private System.Windows.Threading.DispatcherTimer? _resourceTimer;

        private Window? _dockingOverlayWindow;
        private Grid? _overlayDockingGuide;
        private Border? _overlayDockPreview;
        private Border? _guideTop;
        private Border? _guideLeft;
        private Border? _guideCenter;
        private Border? _guideRight;
        private Border? _guideBottom;

        private Grid? _panelDockingGuide;
        private Border? _panelGuideTop;
        private Border? _panelGuideLeft;
        private Border? _panelGuideCenter;
        private Border? _panelGuideRight;
        private Border? _panelGuideBottom;
        private TabControl? _hoveredTabControl;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCpuCounter();
            InitializeWebView();
            Engine.ThemeManager.ThemeChanged += OnThemeChanged;

            LeftDockTabControl.SelectionChanged += DockTabControl_SelectionChanged;
            RightDockTabControl.SelectionChanged += DockTabControl_SelectionChanged;
            TopDockTabControl.SelectionChanged += DockTabControl_SelectionChanged;
            BottomDockTabControl.SelectionChanged += DockTabControl_SelectionChanged;
        }

        public void DockTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDockedTabsTheme();
        }

        private async void InitializeWebView()
        {
            try
            {
                // Ensure nodes directory exists in base directory
                _nodesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nodes");
                if (!Directory.Exists(_nodesDirectory))
                {
                    Directory.CreateDirectory(_nodesDirectory);
                    CreateDefaultSampleNodes();
                }

                // Initialize WebView2
                await webView.EnsureCoreWebView2Async();

                // Map virtual host name "app.flowengine.local" to local folder "wwwroot"
                string wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                if (!Directory.Exists(wwwrootPath))
                {
                    Directory.CreateDirectory(wwwrootPath);
                }

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "app.flowengine.local",
                    wwwrootPath,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                // Attach message event handler
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // Load application
                webView.Source = new Uri("https://app.flowengine.local/index.html");

                // Start periodic resource monitoring stream to frontend
                StartResourceMonitoring();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 Initialization Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string messageJson = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(messageJson)) return;

            try
            {
                var doc = JsonDocument.Parse(messageJson);
                var root = doc.RootElement;
                
                string type = root.GetProperty("type").GetString() ?? "";
                string requestId = (root.TryGetProperty("requestId", out var rId) ? rId.GetString() : null) ?? "";
                var payload = root.TryGetProperty("payload", out var pLoad) ? pLoad : (JsonElement?)null;

                await HandleMessageAsync(type, requestId, payload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // Suppress: throw/logCallback/MessageBox not needed here
            }
        }

        private async Task HandleMessageAsync(string type, string requestId, JsonElement? payload)
        {
            object? responsePayload = null;
            string? errorMsg = null;

            try
            {
                switch (type)
                {
                    case "GET_NODE_LIBRARY":
                        responsePayload = GetNodeLibrary();
                        break;

                    case "RUN_FLOW":
                        if (payload.HasValue)
                        {
                            var outputs = await Task.Run(() => RunFlow(payload.Value));
                            responsePayload = new { success = true, outputs = outputs };
                        }
                        else
                        {
                            errorMsg = "Missing graph payload.";
                        }
                        break;

                    case "STOP_FLOW":
                        FlowEngine.Engine.FlowExecutionManager.StopRequested = true;
                        _ = SendToHostAsync(new
                        {
                            type = "LOG_PRINTED",
                            payload = new { level = "WARN", message = "Stop request received. Halting execution..." }
                        });
                        LuaRunner.WriteLogToFile("WARN", "Stop request received. Halting execution...");
                        responsePayload = new { success = true };
                        break;

                    case "SAVE_PROJECT":
                        if (payload.HasValue)
                        {
                            string jsonStr = JsonSerializer.Serialize(payload.Value);
                            string? savedPath = ProjectManager.SaveProject(jsonStr);
                            responsePayload = new { success = savedPath != null, filePath = savedPath };
                        }
                        else
                        {
                            errorMsg = "Missing project payload.";
                        }
                        break;

                    case "LOAD_PROJECT":
                        string? content = null;
                        string? loadedPath = ProjectManager.LoadProject(out content);
                        if (loadedPath != null && content != null)
                        {
                            using (var doc = JsonDocument.Parse(content))
                            {
                                responsePayload = new { success = true, filePath = loadedPath, data = doc.RootElement.Clone() };
                            }
                        }
                        else
                        {
                            responsePayload = new { success = false };
                        }
                        break;

                    case "SAVE_THEME":
                        if (payload.HasValue)
                        {
                            string jsonStr = JsonSerializer.Serialize(payload.Value);
                            string? savedThemePath = ProjectManager.SaveTheme(jsonStr);
                            responsePayload = new { success = savedThemePath != null, filePath = savedThemePath };
                        }
                        else
                        {
                            errorMsg = "Missing theme payload.";
                        }
                        break;

                    case "LOAD_THEME":
                        string? themeContent = null;
                        string? loadedThemePath = ProjectManager.LoadTheme(out themeContent);
                        if (loadedThemePath != null && themeContent != null)
                        {
                            using (var doc = JsonDocument.Parse(themeContent))
                            {
                                responsePayload = new { success = true, filePath = loadedThemePath, data = doc.RootElement.Clone() };
                            }
                        }
                        else
                        {
                            responsePayload = new { success = false };
                        }
                        break;

                    case "THEME_CHANGED":
                        if (payload.HasValue)
                        {
                            try
                            {
                                string titleBarBg = payload.Value.GetProperty("titleBarBg").GetString() ?? "#1e1e2e";
                                string titleBarFg = payload.Value.GetProperty("titleBarFg").GetString() ?? "#cdd6f4";
                                string borderCol = payload.Value.GetProperty("borderColor").GetString() ?? "#313244";

                                string? panelBg = payload.Value.TryGetProperty("panelBg", out var pBg) ? pBg.GetString() : null;
                                string? dialogHeaderBg = payload.Value.TryGetProperty("dialogHeaderBg", out var dhBg) ? dhBg.GetString() : null;
                                string? dialogHeaderFg = payload.Value.TryGetProperty("dialogHeaderFg", out var dhFg) ? dhFg.GetString() : null;
                                string? textMuted = payload.Value.TryGetProperty("textMuted", out var tMuted) ? tMuted.GetString() : null;

                                Engine.ThemeManager.ApplyTheme(titleBarBg, titleBarFg, borderCol, panelBg, dialogHeaderBg, dialogHeaderFg, textMuted);
                            }
                            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // Suppress: throw/logCallback/MessageBox not needed here
            }
                        }
                        responsePayload = new { success = true };
                        break;

                    case "EXIT_APP":
                        Application.Current.Shutdown();
                        break;

                    default:
                        errorMsg = $"Unknown command: {type}";
                        break;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            if (!string.IsNullOrEmpty(requestId))
            {
                // Send response back
                var response = new
                {
                    type = type + "_RESPONSE",
                    requestId = requestId,
                    payload = responsePayload,
                    error = errorMsg
                };

                await SendToHostAsync(response);
            }
        }

        private object GetNodeLibrary()
        {
            if (!Directory.Exists(_nodesDirectory))
                return Array.Empty<object>();

            var luaFiles = Directory.GetFiles(_nodesDirectory, "*.lua", SearchOption.AllDirectories);
            var nodes = luaFiles.Select(file =>
            {
                try
                {
                    string content = File.ReadAllText(file);
                    var metadata = LuaParser.Parse(content);
                    string relPath = Path.GetRelativePath(_nodesDirectory, file).Replace('\\', '/');
                    return new
                    {
                        id = metadata.Id,
                        name = metadata.Name,
                        description = metadata.Description,
                        path = relPath,
                        inputs = metadata.Inputs.Select(i => new { name = i.Name, type = i.Type, defaultValue = i.DefaultValue }),
                        outputs = metadata.Outputs.Select(o => new { name = o.Name, type = o.Type }),
                        script = content
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing node file {file}: {ex.Message}");
                    // Suppress: throw/logCallback/MessageBox not needed here
                    return null;
                }
            }).Where(n => n != null).ToList();

            return nodes;
        }

        private Dictionary<string, Dictionary<string, object>> RunFlow(JsonElement payload)
        {
            try
            {
                string jsonStr = JsonSerializer.Serialize(payload);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var graph = JsonSerializer.Deserialize<FlowGraph>(jsonStr, options);
                if (graph == null)
                {
                    _ = SendToHostAsync(new
                    {
                        type = "LOG_PRINTED",
                        payload = new { level = "ERROR", message = "Failed to deserialize flow graph." }
                    });
                    return new Dictionary<string, Dictionary<string, object>>();
                }

                // Run Flow
                var runner = new FlowRunner(
                    graph,
                    (level, message) =>
                    {
                        // Log callback
                        if (level == "CLEAR")
                        {
                            _ = SendToHostAsync(new { type = "LOG_CLEARED" });
                        }
                        else
                        {
                            LuaRunner.WriteLogToFile(level, message);
                            _ = SendToHostAsync(new
                            {
                                type = "LOG_PRINTED",
                                payload = new { level = level, message = message }
                            });
                        }
                    },
                    (nodeId, cnt, state) =>
                    {
                        // State callback
                        _ = SendToHostAsync(new
                        {
                            type = "NODE_STATE_CHANGED",
                            payload = new { nodeId = nodeId, cnt = cnt, state = state }
                        });
                    }
                );

                // Check if starting node is specified in the payload
                string? startNodeId = payload.TryGetProperty("startNodeId", out var sNodeId) ? sNodeId.GetString() : null;
                // Run the runner
                runner.Run(startNodeId);

                return runner.ComputedOutputs;
            }
            catch (Exception ex)
            {
                _ = SendToHostAsync(new
                {
                    type = "LOG_PRINTED",
                    payload = new { level = "ERROR", message = $"Flow runner crashed: {ex.Message}" }
                });
                LuaRunner.WriteLogToFile("ERROR", $"Flow runner crashed: {ex.Message}");
                return new Dictionary<string, Dictionary<string, object>>();
            }
        }

        private async Task SendToHostAsync(object data)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    string json = JsonSerializer.Serialize(data);
                    // CoreWebView2 might be null if window is closing or initializing
                    if (webView?.CoreWebView2 != null)
                    {
                        await webView.CoreWebView2.ExecuteScriptAsync($"if(window.onMessageFromHost) window.onMessageFromHost({json});");
                    }
                }
                catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // Suppress: throw/logCallback/MessageBox not needed here
            }
            });
        }

        private void CreateDefaultSampleNodes()
        {
            try
            {
                string addScript = @"-- @node: AddOperation
-- @description: 두 실수를 더합니다.
function add(a : float, b : float) -> c : float
    return a + b
end";
                File.WriteAllText(Path.Combine(_nodesDirectory, "AddOperation.lua"), addScript);

                string multScript = @"-- @node: MultiplyOperation
-- @description: 두 실수를 곱합니다.
function multiply(a : float, b : float) -> c : float
    return a * b
end";
                File.WriteAllText(Path.Combine(_nodesDirectory, "MultiplyOperation.lua"), multScript);

                string printScript = @"-- @node: PrintNode
-- @description: 입력 값을 콘솔 로그에 출력합니다.
function printNode(value : string)
    print(""[Lua Node Output] "" .. tostring(value))
end";
                File.WriteAllText(Path.Combine(_nodesDirectory, "PrintNode.lua"), printScript);

                string loopTestScript = @"-- @node: LoopTestNode
-- @description: 실행할 때마다 카운터를 1씩 증가시켜 누적 합을 계산합니다.
-- @input: value : float, accumulator : float
-- @output: sum : float
function loopTest(value : float, accumulator : float) -> sum : float
    local currentAccum = accumulator or 0
    return currentAccum + value
end";
                File.WriteAllText(Path.Combine(_nodesDirectory, "LoopTestNode.lua"), loopTestScript);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // Suppress: throw/logCallback/MessageBox not needed here
            }
        }

        // Custom Window Titlebar button handlers
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


        private void UpdateTitleBarColors(string bgHex, string fgHex, string borderHex)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var converter = new System.Windows.Media.BrushConverter();
                    var bgBrush = converter.ConvertFromString(bgHex) as System.Windows.Media.Brush;
                    var fgBrush = converter.ConvertFromString(fgHex) as System.Windows.Media.Brush;
                    var borderBrush = converter.ConvertFromString(borderHex) as System.Windows.Media.Brush;

                    if (TitleBarBorder != null)
                    {
                        TitleBarBorder.Background = bgBrush;
                        TitleBarBorder.BorderBrush = borderBrush;
                    }

                    if (TitleTextBlock != null)
                    {
                        TitleTextBlock.Foreground = fgBrush;
                    }
                }
                catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // Suppress: throw/logCallback/MessageBox not needed here
            }
            });
        }

        private void OnThemeChanged()
        {
            Dispatcher.Invoke(() =>
            {
                UpdateTitleBarColors(Engine.ThemeManager.TitleBarBg, Engine.ThemeManager.TitleBarFg, Engine.ThemeManager.BorderColor);
                UpdateDockedTabsTheme();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Unsubscribe from events to prevent memory leaks
            Engine.ThemeManager.ThemeChanged -= OnThemeChanged;
            
            LeftDockTabControl.SelectionChanged -= DockTabControl_SelectionChanged;
            RightDockTabControl.SelectionChanged -= DockTabControl_SelectionChanged;
            TopDockTabControl.SelectionChanged -= DockTabControl_SelectionChanged;
            BottomDockTabControl.SelectionChanged -= DockTabControl_SelectionChanged;
            
            if (webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }
            
            // Close all open image windows
            OpenCvLuaApi.CloseAllImageWindows();
            
            // Shut down the application
            Application.Current.Shutdown();
        }
    }
}
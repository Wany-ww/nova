using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MoonSharp.Interpreter;

namespace FlowEngine.Engine
{
        /// <summary>
    /// Executes Lua node scripts within a sandboxed MoonSharp environment,
    /// providing custom global APIs for cv, time, log, and console.
    /// </summary>
    public static class LuaRunner
    {
        private static bool _logSaveEnabled = false;
        private static string? _logSaveFilePath = null;
        private static readonly object _logFileLock = new object();

        private static readonly Dictionary<string, object?> _globalMemory = new Dictionary<string, object?>();
        private static readonly object _globalMemoryLock = new object();
        private static readonly System.Diagnostics.PerformanceCounter? _cpuCounter;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MEMORYSTATUSEX lpBuffer);

        static LuaRunner()
        {
            UserData.RegisterType<LuaSocket>();
            try
            {
                _cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            catch
            {
                _cpuCounter = null;
            }
        }

                /// <summary>
        /// Configures real-time log saving to a default or specified file path.
        /// </summary>
        /// <param name="enable">True to enable real-time file logging, False to disable</param>
        /// <param name="file">The output file path, or null/empty for default date log path</param>
        public static void ConfigureLogSave(bool enable, string? file)
        {
            if (enable)
            {
                string resolvedPath;
                if (string.IsNullOrEmpty(file))
                {
                    string saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "save");
                    if (!Directory.Exists(saveDir))
                    {
                        Directory.CreateDirectory(saveDir);
                    }
                    string defaultFile = $"{DateTime.Now:yyyyMMdd}_log.txt";
                    resolvedPath = Path.Combine(saveDir, defaultFile);
                }
                else
                {
                    string fullPath = Path.GetFullPath(file);
                    string? dir = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    resolvedPath = fullPath;
                }

                lock (_logFileLock)
                {
                    _logSaveEnabled = true;
                    _logSaveFilePath = resolvedPath;
                }
            }
            else
            {
                lock (_logFileLock)
                {
                    _logSaveEnabled = false;
                    _logSaveFilePath = null;
                }
            }
        }

                /// <summary>
        /// Appends a log line to the configured log file in a thread-safe manner.
        /// </summary>
        /// <param name="level">The severity level of the log message</param>
        /// <param name="message">The text message to log</param>
        public static void WriteLogToFile(string level, string message)
        {
            try
            {
                lock (_logFileLock)
                {
                    if (!_logSaveEnabled || string.IsNullOrEmpty(_logSaveFilePath)) return;

                    string cleanMessage = message.StartsWith("[LUA_PRINT] ") ? message.Substring(12) : message;
                    string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {cleanMessage}{Environment.NewLine}";
                    File.AppendAllText(_logSaveFilePath, logLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write log to file: {ex.Message}");
                // Suppress: throw/logCallback/MessageBox not needed here (prevents infinite loop/recursion)
            }
        }

                /// <summary>
        /// Executes a single node's Lua script with specified inputs and log callbacks.
        /// </summary>
        /// <param name="metadata">The parsed metadata structure of the node script</param>
        /// <param name="inputValues">The input port values mapped by pin name</param>
        /// <param name="logCallback">The callback to stream stdout and logger events</param>
        /// <returns>A dictionary of outputs returned by the executed function</returns>
        public static Dictionary<string, object> Run(
            NodeMetadata metadata, 
            Dictionary<string, object> inputValues, 
            Action<string, string>? logCallback)
        {
            var outputs = new Dictionary<string, object>();

            try
            {
                // Create clean Lua script environment
                var script = new Script();

                // Override global 'print' function to redirect to C# console
                script.Globals["print"] = (Func<ScriptExecutionContext, CallbackArguments, DynValue>)((context, args) =>
                {
                    var list = new List<string>();
                    for (int i = 0; i < args.Count; i++)
                    {
                        list.Add(args[i].ToPrintString() ?? "nil");
                    }
                    string msg = string.Join("\t", list);
                    logCallback?.Invoke("INFO", "[LUA_PRINT] " + msg);
                    return DynValue.Nil;
                });

                // Register global log API
                var logTable = new Table(script);
                logTable["info"] = (Action<object>)(text => 
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    logCallback?.Invoke("INFO", "[LUA_PRINT] " + (text?.ToString() ?? "nil"));
                });
                logTable["warn"] = (Action<object>)(text => 
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    logCallback?.Invoke("WARN", "[LUA_PRINT] " + (text?.ToString() ?? "nil"));
                });
                logTable["error"] = (Action<object>)(text => 
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    logCallback?.Invoke("ERROR", "[LUA_PRINT] " + (text?.ToString() ?? "nil"));
                });
                logTable["clear"] = (Action)(() =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    logCallback?.Invoke("CLEAR", "");
                });
                logTable["save"] = (Func<DynValue, DynValue, DynValue>)((enableVal, fileVal) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    
                    bool enable = enableVal.CastToBool();
                    string? file = null;
                    if (fileVal != null && !fileVal.IsNil() && fileVal.Type == DataType.String)
                    {
                        file = fileVal.String;
                    }
                    
                    ConfigureLogSave(enable, file);
                    return DynValue.Nil;
                });
                script.Globals["log"] = logTable;

                // Register global console API
                var consoleTable = new Table(script);
                consoleTable["clear"] = (Action)(() =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    logCallback?.Invoke("CLEAR", "");
                });
                consoleTable["save"] = (Func<DynValue, DynValue, DynValue>)((enableVal, fileVal) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    
                    bool enable = enableVal.CastToBool();
                    string? file = null;
                    if (fileVal != null && !fileVal.IsNil() && fileVal.Type == DataType.String)
                    {
                        file = fileVal.String;
                    }
                    
                    ConfigureLogSave(enable, file);
                    return DynValue.Nil;
                });
                script.Globals["console"] = consoleTable;

                // Register global time.sleep API
                var timeTable = new Table(script);
                var sleepTable = new Table(script);
                sleepTable["sec"] = (Action<double>)(t => 
                {
                    int ms = (int)(t * 1000);
                    int step = 50;
                    for (int elapsed = 0; elapsed < ms; elapsed += step)
                    {
                        if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                        System.Threading.Thread.Sleep(Math.Min(step, ms - elapsed));
                    }
                });
                sleepTable["ms"] = (Action<double>)(t => 
                {
                    int ms = (int)t;
                    int step = 50;
                    for (int elapsed = 0; elapsed < ms; elapsed += step)
                    {
                        if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                        System.Threading.Thread.Sleep(Math.Min(step, ms - elapsed));
                    }
                });
                sleepTable["us"] = (Action<double>)(t => 
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    SleepMicroseconds((long)t);
                });
                sleepTable["micro"] = (Action<double>)(t => 
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    SleepMicroseconds((long)t);
                });
                timeTable["sleep"] = sleepTable;
                script.Globals["time"] = timeTable;

                // Register global variable API
                var variableTable = new Table(script);
                variableTable["set"] = (Action<string, DynValue>)((name, val) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    lock (_globalMemoryLock)
                    {
                        _globalMemory[name] = ConvertDynValue(val);
                    }
                });
                variableTable["get"] = (Func<string, DynValue>)((name) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    object? val = null;
                    lock (_globalMemoryLock)
                    {
                        _globalMemory.TryGetValue(name, out val);
                    }
                    if (val == null)
                    {
                        return DynValue.Nil;
                    }
                    return DynValue.FromObject(script, val);
                });
                script.Globals["variable"] = variableTable;

                // Register global tcp API
                var tcpTable = new Table(script);
                var tcpServerTable = new Table(script);
                var tcpClientTable = new Table(script);
                tcpServerTable["create"] = (Func<int, LuaSocket?>)((port) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return LuaSocket.CreateTcpServer(port);
                });
                tcpClientTable["connect"] = (Func<string, int, LuaSocket?>)((ip, port) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return LuaSocket.ConnectTcpClient(ip, port);
                });
                tcpTable["server"] = tcpServerTable;
                tcpTable["client"] = tcpClientTable;
                script.Globals["tcp"] = tcpTable;

                // Register global udp API
                var udpTable = new Table(script);
                var udpServerTable = new Table(script);
                var udpClientTable = new Table(script);
                udpServerTable["create"] = (Func<int, LuaSocket?>)((port) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return LuaSocket.CreateUdpServer(port);
                });
                udpClientTable["connect"] = (Func<string, int, LuaSocket?>)((ip, port) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return LuaSocket.ConnectUdpClient(ip, port);
                });
                udpTable["server"] = udpServerTable;
                udpTable["client"] = udpClientTable;
                script.Globals["udp"] = udpTable;

                // Register global filesystem API
                var fsTable = new Table(script);
                fsTable["current"] = (Func<string>)(() =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return Directory.GetCurrentDirectory();
                });
                fsTable["remove"] = (Action<string>)((path) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                });
                fsTable["create"] = (Action<string>)((path) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    Directory.CreateDirectory(path);
                });
                fsTable["is_exist"] = (Func<string, bool>)((file) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return File.Exists(file) || Directory.Exists(file);
                });
                fsTable["copy"] = (Action<string, string>)((src, dst) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    if (File.Exists(src))
                    {
                        string? dir = Path.GetDirectoryName(dst);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.Copy(src, dst, true);
                    }
                    else if (Directory.Exists(src))
                    {
                        CopyDirectory(src, dst);
                    }
                });
                script.Globals["filesystem"] = fsTable;

                // Register global http API
                var httpTable = new Table(script);
                httpTable["get"] = (Func<string, Table?, Table>)((url, headers) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return HttpGet(script, url, headers);
                });
                httpTable["post"] = (Func<string, string, Table?, Table>)((url, body, headers) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return HttpPost(script, url, body, headers);
                });
                httpTable["download"] = (Func<string, string, bool>)((url, destPath) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return HttpDownload(url, destPath);
                });
                script.Globals["http"] = httpTable;

                // Register global json API
                var jsonTable = new Table(script);
                jsonTable["parse"] = (Func<string, DynValue>)((jsonStr) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return JsonParse(script, jsonStr);
                });
                jsonTable["stringify"] = (Func<DynValue, string>)((val) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return JsonStringify(val);
                });
                script.Globals["json"] = jsonTable;

                // Register global system API
                var systemTable = new Table(script);
                systemTable["run"] = (Func<string, Table?, DynValue>)((command, argsTable) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return SystemRun(script, command, argsTable);
                });
                systemTable["notify"] = (Action<string, string, string>)((title, message, type) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    TrayNotification.Show(title, message, type);
                });
                systemTable["cpu_usage"] = (Func<double>)(() =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return GetCpuUsage();
                });
                systemTable["ram_usage"] = (Func<Table>)(() =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return GetRamUsage(script);
                });
                systemTable["disk_free"] = (Func<string, double>)((drive) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return GetDiskFreeSpace(drive);
                });
                systemTable["speak"] = (Action<string>)((text) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    SystemSpeak(text);
                });
                script.Globals["system"] = systemTable;

                // Register global crypto API
                var cryptoTable = new Table(script);
                cryptoTable["sha256"] = (Func<string, string>)((str) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return Sha256(str);
                });
                cryptoTable["md5"] = (Func<string, string>)((str) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return Md5(str);
                });
                cryptoTable["base64_encode"] = (Func<string, string>)((str) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return Base64Encode(str);
                });
                cryptoTable["base64_decode"] = (Func<string, string>)((str) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return Base64Decode(str);
                });
                script.Globals["crypto"] = cryptoTable;

                // Register global csv API
                var csvTable = new Table(script);
                csvTable["read"] = (Func<string, Table>)((file) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return CsvRead(script, file);
                });
                csvTable["write"] = (Func<string, Table, bool>)((file, data) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return CsvWrite(file, data);
                });
                script.Globals["csv"] = csvTable;


                // Register global ftp API
                var ftpTable = new Table(script);
                ftpTable["upload"] = (Func<string, int, string, string, string, string, bool>)((host, port, user, pass, localFile, remoteFile) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return FtpUpload(host, port, user, pass, localFile, remoteFile);
                });
                ftpTable["download"] = (Func<string, int, string, string, string, string, bool>)((host, port, user, pass, remoteFile, localFile) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    return FtpDownload(host, port, user, pass, remoteFile, localFile);
                });
                script.Globals["ftp"] = ftpTable;

                // Register global input API
                var inputTable = new Table(script);
                inputTable["mouse_move"] = (Action<int, int>)((x, y) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    InputAutomation.MouseMove(x, y);
                });
                inputTable["mouse_click"] = (Action<string>)((btn) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    InputAutomation.MouseClick(btn);
                });
                inputTable["key_press"] = (Action<int>)((keyCode) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    InputAutomation.KeyPress(keyCode);
                });
                inputTable["key_type"] = (Action<string>)((text) =>
                {
                    if (FlowExecutionManager.StopRequested) throw new ScriptRuntimeException("Execution stopped by user.");
                    InputAutomation.KeyType(text);
                });
                script.Globals["input"] = inputTable;


                // Register global cv API
                OpenCvLuaApi.Register(script);


                // Load custom script
                script.DoString(metadata.CleanScript);

                // Get function reference
                var func = script.Globals.Get(metadata.FunctionName);
                if (func == null || func.Type != DataType.Function)
                {
                    throw new Exception($"Function '{metadata.FunctionName}' not found or is not a valid function.");
                }

                // Prepare inputs in order of declaration
                var argsList = new List<object>();
                foreach (var inputPin in metadata.Inputs)
                {
                    if (inputValues.TryGetValue(inputPin.Name, out var val))
                    {
                        argsList.Add(ConvertInput(val, inputPin.Type) ?? DynValue.Nil);
                    }
                    else
                    {
                        argsList.Add(inputPin.DefaultValue ?? DynValue.Nil);
                    }
                }

                // Execute the function
                DynValue res = script.Call(func, argsList.ToArray());

                // Parse outputs
                if (metadata.Outputs.Count > 0)
                {
                    if (res.Type == DataType.Tuple)
                    {
                        // Multiple returns e.g. return a, b
                        var tuple = res.Tuple;
                        for (int i = 0; i < metadata.Outputs.Count; i++)
                        {
                            var outPin = metadata.Outputs[i];
                            if (i < tuple.Length)
                            {
                                outputs[outPin.Name] = ConvertOutput(tuple[i], outPin.Type) ?? DynValue.Nil;
                            }
                            else
                            {
                                outputs[outPin.Name] = outPin.DefaultValue ?? DynValue.Nil;
                            }
                        }
                    }
                    else
                    {
                        // Single return
                        var outPin = metadata.Outputs[0];
                        outputs[outPin.Name] = ConvertOutput(res, outPin.Type) ?? DynValue.Nil;

                        // Fill rest with default values if any
                        for (int i = 1; i < metadata.Outputs.Count; i++)
                        {
                            outputs[metadata.Outputs[i].Name] = metadata.Outputs[i].DefaultValue ?? DynValue.Nil;
                        }
                    }
                }
            }
            catch (InterpreterException ex)
            {
                logCallback?.Invoke("ERROR", $"Lua Runtime Error: {ex.DecoratedMessage ?? ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                logCallback?.Invoke("ERROR", $"Execution Error: {ex.Message}");
                throw;
            }

            return outputs;
        }

                /// <summary>
        /// Recursively parses System.Text.Json JsonElements into standard C# types.
        /// </summary>
        /// <param name="val">The raw object, which might be a JsonElement</param>
        /// <returns>The resolved standard C# value</returns>
        private static object? ResolveJsonElement(object? val)
        {
            if (val == null) return null;
            if (val is JsonElement elem)
            {
                switch (elem.ValueKind)
                {
                    case JsonValueKind.String:
                        return elem.GetString();
                    case JsonValueKind.Number:
                        if (elem.TryGetInt32(out int i)) return i;
                        return elem.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.Object:
                        var dict = new Dictionary<string, object?>();
                        foreach (var prop in elem.EnumerateObject())
                        {
                            dict[prop.Name] = ResolveJsonElement(prop.Value);
                        }
                        return dict;
                    case JsonValueKind.Array:
                        var list = new List<object?>();
                        foreach (var item in elem.EnumerateArray())
                        {
                            list.Add(ResolveJsonElement(item));
                        }
                        return list;
                    default:
                        return null;
                }
            }
            return val;
        }

                /// <summary>
        /// Converts incoming raw input values into target types expected by Lua functions.
        /// </summary>
        /// <param name="val">The raw input value</param>
        /// <param name="type">The target datatype string</param>
        /// <returns>The cast/converted C# object</returns>
        private static object? ConvertInput(object? val, string type)
        {
            if (val == null) return null;

            val = ResolveJsonElement(val);
            if (val == null) return null;

            try
            {
                switch (type.ToLower())
                {
                    case "int":
                        return Convert.ToInt32(val);
                    case "float":
                        return Convert.ToDouble(val); // Lua numbers are internally doubles
                    case "bool":
                        return Convert.ToBoolean(val);
                    case "string":
                        return val.ToString();
                    case "table":
                        // Convert JSON element / C# Dictionary to Lua table if possible
                        // MoonSharp automatically marshalls standard dictionaries
                        return val;
                    default:
                        return val;
                }
            }
            catch
            {
                // Fallback to raw value
                return val;
            }
        }

                /// <summary>
        /// Converts MoonSharp DynValues returned from Lua back into standard C# datatypes.
        /// </summary>
        /// <param name="val">The DynValue returned by MoonSharp</param>
        /// <param name="type">The output pin target type string</param>
        /// <returns>The converted C# object</returns>
        private static object? ConvertOutput(DynValue? val, string type)
        {
            if (val == null || val.IsNil()) return null;

            switch (type.ToLower())
            {
                case "int":
                    return (int)val.Number;
                case "float":
                    return val.Number;
                case "bool":
                    return val.Boolean;
                case "string":
                    return val.String ?? val.ToString();
                case "table":
                    if (val.Type == DataType.Table)
                    {
                        return TableToDictionary(val.Table);
                    }
                    if (val.Type == DataType.UserData)
                    {
                        return val.ToObject();
                    }
                    return null;
                default:
                    return val.ToObject();
            }
        }

                /// <summary>
        /// Helper to marshal MoonSharp Tables recursively into standard C# Dictionaries.
        /// </summary>
        /// <param name="table">The MoonSharp Table to convert</param>
        /// <returns>A dictionary containing the parsed table entries</returns>
        private static Dictionary<string, object?> TableToDictionary(Table table)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var pair in table.Pairs)
            {
                string key = pair.Key.Type == DataType.String ? pair.Key.String : (pair.Key.ToObject()?.ToString() ?? "");
                object? valObj = null;
                if (pair.Value.Type == DataType.Table)
                {
                    valObj = TableToDictionary(pair.Value.Table);
                }
                else
                {
                    valObj = pair.Value.ToObject();
                }
                dict[key] = valObj;
            }
            return dict;
        }

                /// <summary>
        /// Performs a high-precision busy-wait sleep for microseconds.
        /// </summary>
        /// <param name="microseconds">The number of microseconds to sleep</param>
        private static void SleepMicroseconds(long microseconds)
        {
            if (microseconds <= 0) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long ticksToWait = microseconds * System.Diagnostics.Stopwatch.Frequency / 1000000;
            while (sw.ElapsedTicks < ticksToWait)
            {
                System.Threading.Thread.SpinWait(10);
            }
        }

        /// <summary>
        /// Clears all variables stored in the global memory dictionary.
        /// </summary>
        public static void ClearGlobalMemory()
        {
            lock (_globalMemoryLock)
            {
                _globalMemory.Clear();
            }
        }

        /// <summary>
        /// Converts MoonSharp DynValues to standard C# values for global variable storage.
        /// </summary>
        private static object? ConvertDynValue(DynValue val)
        {
            if (val == null || val.IsNil()) return null;
            switch (val.Type)
            {
                case DataType.Boolean:
                    return val.Boolean;
                case DataType.Number:
                    return val.Number;
                case DataType.String:
                    return val.String;
                case DataType.Table:
                    return TableToCSharp(val.Table);
                default:
                    return val.ToObject();
            }
        }

        /// <summary>
        /// Recursively converts a MoonSharp Table into standard C# collections (List or Dictionary).
        /// </summary>
        private static object TableToCSharp(Table table)
        {
            bool isSequence = true;
            int count = 0;
            foreach (var pair in table.Pairs)
            {
                count++;
            }
            
            for (int i = 1; i <= count; i++)
            {
                var item = table.Get(i);
                if (item.IsNil())
                {
                    isSequence = false;
                    break;
                }
            }
            
            if (isSequence && count > 0)
            {
                var list = new List<object?>();
                for (int i = 1; i <= count; i++)
                {
                    var val = table.Get(i);
                    if (val.Type == DataType.Table)
                        list.Add(TableToCSharp(val.Table));
                    else
                        list.Add(val.ToObject());
                }
                return list;
            }
            else
            {
                var dict = new Dictionary<string, object?>();
                foreach (var pair in table.Pairs)
                {
                    string key = pair.Key.Type == DataType.String ? pair.Key.String : (pair.Key.ToObject()?.ToString() ?? "");
                    object? valObj = null;
                    if (pair.Value.Type == DataType.Table)
                    {
                        valObj = TableToCSharp(pair.Value.Table);
                    }
                    else
                    {
                        valObj = pair.Value.ToObject();
                    }
                    dict[key] = valObj;
                }
                return dict;
            }
        }

        /// <summary>
        /// Helper to copy directory content recursively.
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;
            Directory.CreateDirectory(destinationDir);
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        /// <summary>
        /// Sends an HTTP GET request to the specified URL with optional request headers.
        /// </summary>
        private static Table HttpGet(Script script, string url, Table? headers)
        {
            var resTable = new Table(script);
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                    if (headers != null)
                    {
                        foreach (var pair in headers.Pairs)
                        {
                            string key = pair.Key.String;
                            if (key != null)
                            {
                                request.Headers.TryAddWithoutValidation(key, pair.Value.String ?? pair.Value.ToString());
                            }
                        }
                    }
                    var response = client.SendAsync(request).Result;
                    resTable["status"] = (int)response.StatusCode;
                    resTable["body"] = response.Content.ReadAsStringAsync().Result;
                    
                    var resHeaders = new Table(script);
                    foreach (var header in response.Headers)
                    {
                        resHeaders[header.Key] = string.Join(", ", header.Value);
                    }
                    foreach (var header in response.Content.Headers)
                    {
                        resHeaders[header.Key] = string.Join(", ", header.Value);
                    }
                    resTable["headers"] = resHeaders;
                }
            }
            catch (Exception ex)
            {
                resTable["status"] = 0;
                resTable["body"] = "";
                resTable["error"] = ex.InnerException?.Message ?? ex.Message;
            }
            return resTable;
        }

        /// <summary>
        /// Sends an HTTP POST request to the specified URL with a body string and optional request headers.
        /// </summary>
        private static Table HttpPost(Script script, string url, string body, Table? headers)
        {
            var resTable = new Table(script);
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                    request.Content = new System.Net.Http.StringContent(body ?? "", System.Text.Encoding.UTF8);
                    
                    if (headers != null)
                    {
                        foreach (var pair in headers.Pairs)
                        {
                            string key = pair.Key.String;
                            if (key != null)
                            {
                                if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                                {
                                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(pair.Value.String ?? pair.Value.ToString());
                                }
                                else
                                {
                                    request.Headers.TryAddWithoutValidation(key, pair.Value.String ?? pair.Value.ToString());
                                }
                            }
                        }
                    }
                    var response = client.SendAsync(request).Result;
                    resTable["status"] = (int)response.StatusCode;
                    resTable["body"] = response.Content.ReadAsStringAsync().Result;
                    
                    var resHeaders = new Table(script);
                    foreach (var header in response.Headers)
                    {
                        resHeaders[header.Key] = string.Join(", ", header.Value);
                    }
                    foreach (var header in response.Content.Headers)
                    {
                        resHeaders[header.Key] = string.Join(", ", header.Value);
                    }
                    resTable["headers"] = resHeaders;
                }
            }
            catch (Exception ex)
            {
                resTable["status"] = 0;
                resTable["body"] = "";
                resTable["error"] = ex.InnerException?.Message ?? ex.Message;
            }
            return resTable;
        }

        /// <summary>
        /// Recursively converts a JsonElement into a MoonSharp DynValue.
        /// </summary>
        private static DynValue JsonToDynValue(Script script, System.Text.Json.JsonElement element)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Object:
                    var objTable = new Table(script);
                    foreach (var prop in element.EnumerateObject())
                    {
                        objTable[prop.Name] = JsonToDynValue(script, prop.Value);
                    }
                    return DynValue.NewTable(objTable);
                case System.Text.Json.JsonValueKind.Array:
                    var arrTable = new Table(script);
                    int index = 1;
                    foreach (var item in element.EnumerateArray())
                    {
                        arrTable[index++] = JsonToDynValue(script, item);
                    }
                    return DynValue.NewTable(arrTable);
                case System.Text.Json.JsonValueKind.String:
                    return DynValue.NewString(element.GetString());
                case System.Text.Json.JsonValueKind.Number:
                    return DynValue.NewNumber(element.GetDouble());
                case System.Text.Json.JsonValueKind.True:
                    return DynValue.NewBoolean(true);
                case System.Text.Json.JsonValueKind.False:
                    return DynValue.NewBoolean(false);
                case System.Text.Json.JsonValueKind.Null:
                default:
                    return DynValue.Nil;
            }
        }

        /// <summary>
        /// Parses a JSON string and returns a Lua-compatible representation.
        /// </summary>
        private static DynValue JsonParse(Script script, string jsonStr)
        {
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(jsonStr))
                {
                    return JsonToDynValue(script, doc.RootElement.Clone());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Parse error: {ex.Message}");
                // throw;
                return DynValue.Nil;
            }
        }

        /// <summary>
        /// Serializes a MoonSharp DynValue to a JSON string.
        /// </summary>
        private static string JsonStringify(DynValue val)
        {
            string emptyJson = "{}";
            try
            {
                object? obj = ConvertDynValue(val);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
                };
                return System.Text.Json.JsonSerializer.Serialize(obj, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Stringify error: {ex.Message}");
                // throw;
                return emptyJson;
            }
        }

        /// <summary>
        /// Runs an external system command with optional arguments and returns stdout and exit code.
        /// </summary>
        private static DynValue SystemRun(Script script, string command, Table? argsTable)
        {
            string stdout = "";
            int exitCode = -1;
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = command;
                
                var argList = new List<string>();
                if (argsTable != null)
                {
                    foreach (var pair in argsTable.Pairs)
                    {
                        argList.Add(pair.Value.Type == DataType.String ? pair.Value.String : pair.Value.ToString());
                    }
                }
                
                if (argList.Count > 0)
                {
                    var escapedArgs = argList.Select(a => a.Contains(" ") && !a.StartsWith("\"") ? $"\"{a}\"" : a);
                    psi.Arguments = string.Join(" ", escapedArgs);
                }
                
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        
                        stdout = output + (string.IsNullOrEmpty(error) ? "" : "\n" + error);
                        exitCode = process.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                stdout = "Error: " + ex.Message;
                exitCode = -1;
            }
            
            return DynValue.NewTuple(DynValue.NewString(stdout), DynValue.NewNumber(exitCode));
        }

#pragma warning disable SYSLIB0014
        /// <summary>
        /// Uploads a local file to the specified FTP server.
        /// </summary>
        private static bool FtpUpload(string host, int port, string user, string pass, string localFile, string remoteFile)
        {
            try
            {
                if (port <= 0) port = 21;
                string url = $"ftp://{host}:{port}/{remoteFile.TrimStart('/')}";
                var request = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new System.Net.NetworkCredential(user, pass);
                
                using (var fileStream = System.IO.File.OpenRead(localFile))
                using (var requestStream = request.GetRequestStream())
                {
                    fileStream.CopyTo(requestStream);
                }
                
                using (var response = (System.Net.FtpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == System.Net.FtpStatusCode.CommandOK || 
                           response.StatusCode == System.Net.FtpStatusCode.FileActionOK || 
                           response.StatusCode == System.Net.FtpStatusCode.ClosingData;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FTP Upload error: " + ex.Message);
                // throw;
                return false;
            }
        }

        /// <summary>
        /// Downloads a file from the specified FTP server to the local path.
        /// </summary>
        private static bool FtpDownload(string host, int port, string user, string pass, string remoteFile, string localFile)
        {
            try
            {
                if (port <= 0) port = 21;
                string url = $"ftp://{host}:{port}/{remoteFile.TrimStart('/')}";
                var request = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = System.Net.WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new System.Net.NetworkCredential(user, pass);
                
                using (var response = (System.Net.FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    string? dir = System.IO.Path.GetDirectoryName(localFile);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    using (var fileStream = System.IO.File.Create(localFile))
                    {
                        responseStream.CopyTo(fileStream);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FTP Download error: " + ex.Message);
                // throw;
                return false;
            }
        }
#pragma warning restore SYSLIB0014

        private static double GetCpuUsage()
        {
            if (_cpuCounter == null) return 0.0;
            try
            {
                return _cpuCounter.NextValue();
            }
            catch
            {
                return 0.0;
            }
        }

        private static Table GetRamUsage(Script script)
        {
            var table = new Table(script);
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double total = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double avail = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                double used = total - avail;
                table["totalGb"] = total;
                table["availableGb"] = avail;
                table["usedGb"] = used;
                table["load"] = (double)memStatus.dwMemoryLoad;
            }
            else
            {
                table["totalGb"] = 0.0;
                table["availableGb"] = 0.0;
                table["usedGb"] = 0.0;
                table["load"] = 0.0;
            }
            return table;
        }

        private static double GetDiskFreeSpace(string driveName)
        {
            try
            {
                string root = Path.GetPathRoot(driveName) ?? driveName;
                var driveInfo = new System.IO.DriveInfo(root);
                if (driveInfo.IsReady)
                {
                    return driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Disk free space error: " + ex.Message);
                // throw;
            }
            return 0.0;
        }

        private static void SystemSpeak(string text)
        {
            try
            {
                Type? speakType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (speakType != null)
                {
                    object? speakObj = Activator.CreateInstance(speakType);
                    if (speakObj != null)
                    {
                        speakType.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, speakObj, new object[] { text, 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TTS speak error: " + ex.Message);
                // throw;
            }
        }

        private static string Sha256(string str)
        {
            try
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
                    return string.Concat(bytes.Select(b => b.ToString("x2")));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SHA256 error: " + ex.Message);
                // throw;
                return "";
            }
        }

        private static string Md5(string str)
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
                    return string.Concat(bytes.Select(b => b.ToString("x2")));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MD5 error: " + ex.Message);
                // throw;
                return "";
            }
        }

        private static string Base64Encode(string str)
        {
            try
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Base64Encode error: " + ex.Message);
                // throw;
                return "";
            }
        }

        private static string Base64Decode(string str)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(str);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Base64Decode error: " + ex.Message);
                // throw;
                return "";
            }
        }

        private static Table CsvRead(Script script, string file)
        {
            var table = new Table(script);
            try
            {
                if (File.Exists(file))
                {
                    string[] lines = File.ReadAllLines(file);
                    int rowIndex = 1;
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var columns = ParseCsvLine(line);
                        var rowTable = new Table(script);
                        for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                        {
                            rowTable[colIndex + 1] = columns[colIndex];
                        }
                        table[rowIndex] = rowTable;
                        rowIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CSV Read error: " + ex.Message);
                // throw;
            }
            return table;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentField = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString());
            return result;
        }

        private static bool CsvWrite(string file, Table data)
        {
            try
            {
                var lines = new List<string>();
                int rowCount = 0;
                foreach (var pair in data.Pairs)
                {
                    if (pair.Key.Type == DataType.Number)
                    {
                        int index = (int)pair.Key.Number;
                        if (index > rowCount) rowCount = index;
                    }
                }

                for (int r = 1; r <= rowCount; r++)
                {
                    var rowVal = data.Get(r);
                    if (rowVal.Type == DataType.Table)
                    {
                        var rowTable = rowVal.Table;
                        int colCount = 0;
                        foreach (var pair in rowTable.Pairs)
                        {
                            if (pair.Key.Type == DataType.Number)
                            {
                                int index = (int)pair.Key.Number;
                                if (index > colCount) colCount = index;
                            }
                        }

                        var rowFields = new List<string>();
                        for (int c = 1; c <= colCount; c++)
                        {
                            var colVal = rowTable.Get(c);
                            string valStr = colVal.IsNil() ? "" : (colVal.String ?? colVal.ToObject()?.ToString() ?? "");
                            if (valStr.Contains(",") || valStr.Contains("\"") || valStr.Contains("\n") || valStr.Contains("\r"))
                            {
                                valStr = "\"" + valStr.Replace("\"", "\"\"") + "\"";
                            }
                            rowFields.Add(valStr);
                        }
                        lines.Add(string.Join(",", rowFields));
                    }
                }

                string? dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllLines(file, lines, System.Text.Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CSV Write error: " + ex.Message);
                // throw;
                return false;
            }
        }

        private static bool HttpDownload(string url, string destPath)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var response = client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string? dir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        using (var fileStream = File.Create(destPath))
                        {
                            response.Content.CopyToAsync(fileStream).Wait();
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HTTP Download error: " + ex.Message);
                // throw;
            }
            return false;
        }
    }
}



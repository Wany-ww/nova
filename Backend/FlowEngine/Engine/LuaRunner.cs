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
                string key = pair.Key.ToString();
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
    }
}

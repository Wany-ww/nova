using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlowEngine.Engine
{
    public class NodePin
    {
        public string Name { get; set; }
        public string Type { get; set; } // bool, int, float, string, table, image, etc.
        public object? DefaultValue { get; set; }

        public NodePin(string name, string type)
        {
            Name = name;
            Type = type.Trim().ToLower();
            DefaultValue = GetDefaultValueForType(Type);
        }

        private object? GetDefaultValueForType(string type)
        {
            switch (type)
            {
                case "int": return 0;
                case "float": return 0.0f;
                case "bool": return false;
                case "string": return "";
                case "table": return new Dictionary<string, object>();
                case "image": return ""; // Image path or base64 representation
                default: return null;
            }
        }
    }

    public class NodeMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<NodePin> Inputs { get; set; } = new List<NodePin>();
        public List<NodePin> Outputs { get; set; } = new List<NodePin>();
        public string CleanScript { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
    }

    public static class LuaParser
    {
        public static NodeMetadata Parse(string script)
        {
            var metadata = new NodeMetadata
            {
                Name = "UntitledNode",
                Description = "",
                CleanScript = script
            };

            // 1. Parse @node tag
            var nodeMatch = Regex.Match(script, @"--\s*@node:\s*(\w+)");
            if (nodeMatch.Success)
            {
                metadata.Name = nodeMatch.Groups[1].Value.Trim();
            }
            metadata.Id = metadata.Name;

            // 2. Parse @description tag
            var descMatch = Regex.Match(script, @"--\s*@description:\s*(.*)");
            if (descMatch.Success)
            {
                metadata.Description = descMatch.Groups[1].Value.Trim();
            }

            // 3. Find function declaration
            // e.g., function add(a : float, b : float) -> c : float
            var funcRegex = new Regex(@"function\s+(\w+)\s*\(([^)]*)\)(?:\s*->\s*([^\r\n]+))?", RegexOptions.Multiline);
            var funcMatch = funcRegex.Match(script);

            if (funcMatch.Success)
            {
                string funcName = funcMatch.Groups[1].Value;
                string paramsStr = funcMatch.Groups[2].Value;
                string returnsStr = funcMatch.Groups[3].Value;

                metadata.FunctionName = funcName;

                // Parse Inputs
                var inputsList = new List<string>();
                if (!string.IsNullOrWhiteSpace(paramsStr))
                {
                    var paramParts = paramsStr.Split(',');
                    foreach (var part in paramParts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.Contains(":"))
                        {
                            var subParts = trimmed.Split(':');
                            string pName = subParts[0].Trim();
                            string pType = subParts[1].Trim();
                            metadata.Inputs.Add(new NodePin(pName, pType));
                            inputsList.Add(pName);
                        }
                        else if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            metadata.Inputs.Add(new NodePin(trimmed, "string")); // Default type string
                            inputsList.Add(trimmed);
                        }
                    }
                }

                // Parse Outputs
                if (!string.IsNullOrWhiteSpace(returnsStr))
                {
                    var returnParts = returnsStr.Split(',');
                    foreach (var part in returnParts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.Contains(":"))
                        {
                            var subParts = trimmed.Split(':');
                            string rName = subParts[0].Trim();
                            string rType = subParts[1].Trim();
                            metadata.Outputs.Add(new NodePin(rName, rType));
                        }
                        else if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            metadata.Outputs.Add(new NodePin(trimmed, "string"));
                        }
                    }
                }

                // If @node wasn't found in comments, use function name as Node name/id
                if (metadata.Name == "UntitledNode")
                {
                    metadata.Name = funcName;
                    metadata.Id = funcName;
                }

                // 4. Construct Clean Script (pure Lua function definition)
                // Replace "function func(a : float) -> b : float" with "function func(a)"
                string strippedDecl = $"function {funcName}({string.Join(", ", inputsList)})";
                metadata.CleanScript = funcRegex.Replace(script, strippedDecl);
            }

            // Pre-process cv.8UC1 syntax error in Lua (since identifiers cannot start with numbers)
            metadata.CleanScript = Regex.Replace(metadata.CleanScript ?? "", @"cv\.(\d+\w*)", "cv[\"$1\"]");

            return metadata;
        }
    }
}

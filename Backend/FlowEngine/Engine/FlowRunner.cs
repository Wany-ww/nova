using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FlowEngine.Engine
{
        /// <summary>
    /// Represents an instantiated node configuration in the flow graph,
    /// storing properties, inputs, and its associated Lua execution script.
    /// </summary>
    public class NodeInstance
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();
        public string Script { get; set; } = string.Empty;
    }

        /// <summary>
    /// Represents a connection link between a specific output port of one node
    /// and a specific input port of another node in the flow graph.
    /// </summary>
    public class LinkInstance
    {
        public string FromNode { get; set; } = string.Empty;
        public string FromOutput { get; set; } = string.Empty;
        public string ToNode { get; set; } = string.Empty;
        public string ToInput { get; set; } = string.Empty;
    }

        /// <summary>
    /// Represents the complete graph structure containing nodes and links,
    /// providing helper methods like cycle detection.
    /// </summary>
    public class FlowGraph
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<NodeInstance> Nodes { get; set; } = new List<NodeInstance>();
        public List<LinkInstance> Links { get; set; } = new List<LinkInstance>();

                /// <summary>
        /// Scans the entire graph using depth-first search (DFS) to identify if there are any cycles.
        /// </summary>
        /// <returns>True if a cycle is detected, otherwise False</returns>
        public bool HasCycle()
        {
            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();

            foreach (var node in Nodes)
            {
                if (CheckCycleDFS(node.Id, visited, recStack))
                {
                    return true;
                }
            }
            return false;
        }

                /// <summary>
        /// DFS helper to detect cycles recursively by tracking recursion stacks.
        /// </summary>
        /// <param name="nodeId">The current node identifier to visit</param>
        /// <param name="visited">The set of already visited node identifiers</param>
        /// <param name="recStack">The current recursion stack of node identifiers</param>
        /// <returns>True if a cycle is found in the current branch, otherwise False</returns>
        private bool CheckCycleDFS(string nodeId, HashSet<string> visited, HashSet<string> recStack)
        {
            if (recStack.Contains(nodeId))
            {
                return true;
            }
            if (visited.Contains(nodeId))
            {
                return false;
            }

            visited.Add(nodeId);
            recStack.Add(nodeId);

            var neighbors = Links.Where(l => l.FromNode == nodeId).Select(l => l.ToNode).Distinct();
            foreach (var neighbor in neighbors)
            {
                if (CheckCycleDFS(neighbor, visited, recStack))
                {
                    return true;
                }
            }

            recStack.Remove(nodeId);
            return false;
        }
    }

        /// <summary>
    /// Manages the runtime execution order and cascade of the flow graph,
    /// triggering node executions and passing data between ports.
    /// </summary>
    public class FlowRunner
    {
        private readonly FlowGraph _graph;
        private readonly Action<string, string>? _logCallback; // (level, message)
        private readonly Action<string, int, string>? _stateCallback; // (nodeId, cnt, state)
        private readonly Dictionary<string, Dictionary<string, object>> _computedOutputs;

        public Dictionary<string, Dictionary<string, object>> ComputedOutputs => _computedOutputs;

                /// <summary>
        /// Initializes a new instance of the FlowRunner class.
        /// </summary>
        /// <param name="graph">The target flow graph to execute</param>
        /// <param name="logCallback">The callback action for logging events</param>
        /// <param name="stateCallback">The callback action for node state changes</param>
        public FlowRunner(
            FlowGraph graph, 
            Action<string, string>? logCallback, 
            Action<string, int, string>? stateCallback)
        {
            _graph = graph;
            _logCallback = logCallback;
            _stateCallback = stateCallback;
            _computedOutputs = new Dictionary<string, Dictionary<string, object>>();
        }

                /// <summary>
        /// Begins execution of the flow graph, either starting from entry nodes or a specific starting node.
        /// </summary>
        /// <param name="startNodeId">The optional starting node identifier</param>
        public void Run(string? startNodeId = null)
        {
            // Reset StopRequested on new run
            FlowExecutionManager.StopRequested = false;

            // 1. Cycle Detection
            if (_graph.HasCycle())
            {
                _logCallback?.Invoke("ERROR", "Execution aborted: Cycle detected in the flow graph.");
                return;
            }

            _logCallback?.Invoke("INFO", "Starting flow execution...");

            if (!string.IsNullOrEmpty(startNodeId))
            {
                // Execute starting from a specific node
                ExecuteNode(startNodeId);
            }
            else
            {
                // Find entry nodes.
                // An entry node is a node that:
                // 1. Has no incoming links of any kind, OR
                // 2. Has an outgoing flow connection but NO incoming flow connection (start of flow).
                var allTargetNodes = _graph.Links.Select(l => l.ToNode).ToHashSet();
                var flowTargetNodes = _graph.Links.Where(l => l.ToInput == "flow_in").Select(l => l.ToNode).ToHashSet();
                var nodesWithOutgoingFlow = _graph.Links.Where(l => l.FromOutput == "flow_out").Select(l => l.FromNode).ToHashSet();

                var entryNodes = new List<string>();
                foreach (var node in _graph.Nodes)
                {
                    bool hasIncomingAtAll = allTargetNodes.Contains(node.Id);
                    bool hasIncomingFlow = flowTargetNodes.Contains(node.Id);
                    bool hasOutgoingFlow = nodesWithOutgoingFlow.Contains(node.Id);

                    if (!hasIncomingAtAll || (hasOutgoingFlow && !hasIncomingFlow))
                    {
                        entryNodes.Add(node.Id);
                    }
                }

                if (entryNodes.Count == 0 && _graph.Nodes.Count > 0)
                {
                    entryNodes.Add(_graph.Nodes[0].Id);
                }

                foreach (var nodeId in entryNodes.Distinct())
                {
                    if (FlowExecutionManager.StopRequested) break;
                    ExecuteNode(nodeId);
                }
            }

            if (FlowExecutionManager.StopRequested)
            {
                _logCallback?.Invoke("WARN", "Flow execution halted by user.");
            }
            else
            {
                _logCallback?.Invoke("INFO", "Flow execution completed.");
            }
        }

                /// <summary>
        /// Executes a single node's script, handles loops, inputs resolution, and cascades downstream.
        /// </summary>
        /// <param name="nodeId">The identifier of the node to execute</param>
        private void ExecuteNode(string nodeId)
        {
            if (FlowExecutionManager.StopRequested) return;

            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return;

            // Initialize output structure
            _computedOutputs[nodeId] = new Dictionary<string, object>();

            // Prepare inputs
            var inputValues = new Dictionary<string, object>();

            // 1. Set direct user input values first
            if (node.Inputs != null)
            {
                foreach (var kvp in node.Inputs)
                {
                    inputValues[kvp.Key] = kvp.Value;
                }
            }

            // 2. Overwrite with outputs from incoming links
            var incomingLinks = _graph.Links.Where(l => l.ToNode == nodeId).ToList();
            foreach (var link in incomingLinks)
            {
                if (_computedOutputs.TryGetValue(link.FromNode, out var outputs) && 
                    outputs.TryGetValue(link.FromOutput, out var value))
                {
                    inputValues[link.ToInput] = value;
                }
            }

            // Parse metadata
            NodeMetadata metadata;
            try
            {
                metadata = LuaParser.Parse(node.Script);
            }
            catch (Exception ex)
            {
                _logCallback?.Invoke("ERROR", $"Failed to parse Lua script for node '{nodeId}': {ex.Message}");
                return;
            }

            // Get total count
            int total = 1;
            if (node.Properties != null && node.Properties.TryGetValue("total", out var tVal))
            {
                try
                {
                    if (tVal is JsonElement elem)
                    {
                        if (elem.ValueKind == JsonValueKind.Number)
                        {
                            total = elem.GetInt32();
                        }
                    }
                    else
                    {
                        // JSON numbers might be double or JsonElement
                        total = Convert.ToInt32(tVal);
                    }
                }
                catch
                {
                    total = 1;
                }
            }

            Dictionary<string, object>? outputsResult = null;

            _logCallback?.Invoke("INFO", $"Executing node '{nodeId}' ({node.Type}) [total={total}]");

            // Loop execution (total == 0 means infinite loop)
            bool isInfinite = (total == 0);
            int cnt = 1;
            while (isInfinite || cnt <= total)
            {
                if (FlowExecutionManager.StopRequested)
                {
                    _stateCallback?.Invoke(nodeId, cnt, "IDLE");
                    _logCallback?.Invoke("WARN", $"Node '{nodeId}' execution stopped by user.");
                    return;
                }

                _stateCallback?.Invoke(nodeId, cnt, "RUNNING");
                try
                {
                    outputsResult = LuaRunner.Run(metadata, inputValues, _logCallback!);
                }
                catch (Exception ex)
                {
                    _stateCallback?.Invoke(nodeId, cnt, FlowExecutionManager.StopRequested ? "IDLE" : "ERROR");
                    if (FlowExecutionManager.StopRequested)
                    {
                        _logCallback?.Invoke("WARN", $"Node '{nodeId}' execution stopped by user.");
                    }
                    else
                    {
                        _logCallback?.Invoke("ERROR", $"Execution failed at node '{nodeId}' on iteration {cnt}. Cascade halted. Error: {ex.Message}");
                    }
                    return;
                }
                _stateCallback?.Invoke(nodeId, cnt, "IDLE");

                if (outputsResult != null)
                {
                    _computedOutputs[nodeId] = outputsResult;

                    // Propagate to downstream flow nodes inside the loop
                    var outgoingFlowLinks = _graph.Links.Where(l => l.FromNode == nodeId && l.FromOutput == "flow_out" && l.ToInput == "flow_in").ToList();
                    var downstreamFlowIds = outgoingFlowLinks.Select(l => l.ToNode).Distinct().ToList();

                    foreach (var downId in downstreamFlowIds)
                    {
                        if (FlowExecutionManager.StopRequested) break;
                        ExecuteNode(downId);
                    }
                }

                if (isInfinite)
                {
                    for (int s = 0; s < 10; s++)
                    {
                        if (FlowExecutionManager.StopRequested)
                        {
                            _stateCallback?.Invoke(nodeId, cnt, "IDLE");
                            _logCallback?.Invoke("WARN", $"Node '{nodeId}' loop stopped by user.");
                            return;
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                }

                cnt++;
            }

            if (FlowExecutionManager.StopRequested) return;

            // Propagate to downstream data nodes (nodes that do NOT have flow inputs) after the entire loop completes
            var outgoingLinks = _graph.Links.Where(l => l.FromNode == nodeId).ToList();
            var downstreamDataIds = new List<string>();

            foreach (var link in outgoingLinks)
            {
                string downId = link.ToNode;
                bool downHasFlowInput = _graph.Links.Any(l => l.ToNode == downId && l.ToInput == "flow_in");

                if (!downHasFlowInput)
                {
                    downstreamDataIds.Add(downId);
                }
            }

            foreach (var downId in downstreamDataIds.Distinct())
            {
                if (FlowExecutionManager.StopRequested) break;
                ExecuteNode(downId);
            }
        }
    }
}

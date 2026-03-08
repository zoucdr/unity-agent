using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Model Context Protocol (MCP) client.
    /// Communicates with MCP servers via JSON-RPC 2.0 over HTTP.
    /// Supports tools, resources, and prompts with dynamic loading.
    /// Individual tools can be toggled on/off at runtime.
    /// </summary>
    public class MCPClient : MonoBehaviour
    {
        [SerializeField] private AIAgentConfig config;

        private readonly Dictionary<string, MCPServerConnection> connections =
            new Dictionary<string, MCPServerConnection>();

        private readonly List<MCPTool> availableTools = new List<MCPTool>();
        private readonly List<MCPResource> availableResources = new List<MCPResource>();
        private readonly List<MCPPrompt> availablePrompts = new List<MCPPrompt>();

        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private int rpcIdCounter = 1;

        public bool IsConnected => connections.Values.Any(c => c.IsConnected);
        public List<MCPTool> AvailableTools => availableTools;
        public List<MCPResource> AvailableResources => availableResources;
        public List<MCPPrompt> AvailablePrompts => availablePrompts;

        // ------------------------------------------------------------------ lifecycle

        private void Start()
        {
            ConnectToServers();
        }

        // ------------------------------------------------------------------ connection

        /// <summary>Connect to all enabled MCP servers defined in the config.</summary>
        public async void ConnectToServers()
        {
            if (config == null)
            {
                Debug.LogWarning("[MCPClient] No config set");
                return;
            }

            foreach (var serverConfig in config.MCPServers)
            {
                if (serverConfig.Enabled)
                    await ConnectToServer(serverConfig);
            }
        }

        private async Task ConnectToServer(MCPServerConfig serverConfig)
        {
            try
            {
                Debug.Log($"[MCPClient] Connecting to MCP server: {serverConfig.Name} at {serverConfig.Url}");

                // JSON-RPC initialize request
                var initResult = await SendRpcRequest(serverConfig.Url, "initialize",
                    new { protocolVersion = "2024-11-05", clientInfo = new { name = "unity-agent", version = "1.0.0" } });

                var connection = new MCPServerConnection
                {
                    Name = serverConfig.Name,
                    Url = serverConfig.Url,
                    IsConnected = true
                };

                connections[serverConfig.Name] = connection;

                // Fetch tools, resources, prompts
                await FetchServerCapabilities(connection);

                Debug.Log($"[MCPClient] Connected to MCP server: {serverConfig.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPClient] Could not connect to '{serverConfig.Name}': {ex.Message}");
            }
        }

        // ------------------------------------------------------------------ capabilities

        /// <summary>Re-fetch tools, resources, and prompts from all connected servers.</summary>
        public async void RefreshCapabilities()
        {
            availableTools.Clear();
            availableResources.Clear();
            availablePrompts.Clear();

            foreach (var conn in connections.Values)
            {
                if (conn.IsConnected)
                    await FetchServerCapabilities(conn);
            }
        }

        private async Task FetchServerCapabilities(MCPServerConnection connection)
        {
            await FetchTools(connection);
            await FetchResources(connection);
            await FetchPrompts(connection);
        }

        private async Task FetchTools(MCPServerConnection connection)
        {
            try
            {
                var result = await SendRpcRequest(connection.Url, "tools/list", new { });
                if (result == null) return;

                // result is the JSON string of the "result" field
                var list = ParseToolsList(result, connection.Name);
                availableTools.AddRange(list);

                // Apply config toggles
                ApplyToolToggles();

                Debug.Log($"[MCPClient] Fetched {list.Count} tool(s) from {connection.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPClient] Could not fetch tools from '{connection.Name}': {ex.Message}");
            }
        }

        private async Task FetchResources(MCPServerConnection connection)
        {
            try
            {
                var result = await SendRpcRequest(connection.Url, "resources/list", new { });
                if (result == null) return;

                var list = ParseResourcesList(result, connection.Name);
                availableResources.AddRange(list);

                Debug.Log($"[MCPClient] Fetched {list.Count} resource(s) from {connection.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPClient] Could not fetch resources from '{connection.Name}': {ex.Message}");
            }
        }

        private async Task FetchPrompts(MCPServerConnection connection)
        {
            try
            {
                var result = await SendRpcRequest(connection.Url, "prompts/list", new { });
                if (result == null) return;

                var list = ParsePromptsList(result, connection.Name);
                availablePrompts.AddRange(list);

                Debug.Log($"[MCPClient] Fetched {list.Count} prompt(s) from {connection.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPClient] Could not fetch prompts from '{connection.Name}': {ex.Message}");
            }
        }

        // ------------------------------------------------------------------ runtime toggles

        /// <summary>
        /// Enable or disable an MCP tool by name at runtime.
        /// Also updates the corresponding entry in <see cref="AIAgentConfig.MCPToolToggles"/>.
        /// </summary>
        public void SetToolEnabled(string toolName, bool enabled)
        {
            var tool = availableTools.FirstOrDefault(t => t.Name == toolName);
            if (tool != null)
                tool.Enabled = enabled;

            if (config != null)
            {
                var toggle = config.MCPToolToggles.FirstOrDefault(t => t.ToolName == toolName);
                if (toggle != null)
                    toggle.Enabled = enabled;
                else
                    config.MCPToolToggles.Add(new MCPToolToggle { ToolName = toolName, Enabled = enabled });
            }

            Debug.Log($"[MCPClient] Tool '{toolName}' set enabled={enabled}");
        }

        /// <summary>Returns whether the named tool is currently enabled.</summary>
        public bool IsToolEnabled(string toolName)
        {
            var tool = availableTools.FirstOrDefault(t => t.Name == toolName);
            return tool?.Enabled ?? false;
        }

        // ------------------------------------------------------------------ execution

        /// <summary>Execute a task by finding and calling the appropriate enabled MCP tool.</summary>
        public async Task<string> ExecuteTask(AgentTask task)
        {
            var tool = FindToolForTask(task);

            if (tool != null)
            {
                try
                {
                    return await CallMCPTool(tool, task);
                }
                catch (Exception ex)
                {
                    throw new Exception($"MCP tool '{tool.Name}' execution failed: {ex.Message}", ex);
                }
            }

            throw new Exception($"No enabled MCP tool found for task: {task.Description}");
        }

        // ------------------------------------------------------------------ resource access

        /// <summary>
        /// Read the content of a resource by its URI from the appropriate server.
        /// </summary>
        public async Task<string> ReadResource(string resourceUri)
        {
            var resource = availableResources.FirstOrDefault(r => r.Uri == resourceUri || r.Name == resourceUri);
            if (resource == null)
                throw new Exception($"Resource '{resourceUri}' not found");

            if (!connections.TryGetValue(resource.ServerName, out var connection))
                throw new Exception($"Server '{resource.ServerName}' not connected");

            var result = await SendRpcRequest(connection.Url, "resources/read",
                new { uri = resource.Uri });

            return result ?? string.Empty;
        }

        // ------------------------------------------------------------------ prompt loading

        /// <summary>
        /// Dynamically load a prompt by name, substituting <paramref name="arguments"/>.
        /// Returns the rendered prompt text.
        /// </summary>
        public async Task<string> GetPrompt(string promptName, Dictionary<string, string> arguments = null)
        {
            var prompt = availablePrompts.FirstOrDefault(p => p.Name == promptName);
            if (prompt == null)
                throw new Exception($"Prompt '{promptName}' not found");

            if (!connections.TryGetValue(prompt.ServerName, out var connection))
                throw new Exception($"Server '{prompt.ServerName}' not connected");

            object rpcParams;
            if (arguments != null && arguments.Count > 0)
                rpcParams = new { name = promptName, arguments };
            else
                rpcParams = new { name = promptName };

            var result = await SendRpcRequest(connection.Url, "prompts/get", rpcParams);
            return result ?? string.Empty;
        }

        /// <summary>Disconnect from all servers and clear cached capabilities.</summary>
        public void DisconnectAll()
        {
            connections.Clear();
            availableTools.Clear();
            availableResources.Clear();
            availablePrompts.Clear();
            Debug.Log("[MCPClient] Disconnected from all MCP servers");
        }

        // ------------------------------------------------------------------ JSON-RPC helpers

        /// <summary>
        /// Send a JSON-RPC 2.0 request to <paramref name="serverUrl"/> and return the
        /// raw JSON string of the "result" field, or null on error.
        /// </summary>
        private async Task<string> SendRpcRequest(string serverUrl, string method, object rpcParams)
        {
            var id = rpcIdCounter++;
            var requestObj = new MCPRpcRequest
            {
                jsonrpc = "2.0",
                id = id,
                method = method,
                paramsJson = rpcParams != null ? SimpleJson(rpcParams) : "{}"
            };

            var body = BuildRpcJson(requestObj);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var url = serverUrl.TrimEnd('/') + "/";
            var response = await httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[MCPClient] RPC '{method}' returned HTTP {(int)response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return ExtractResultField(json);
        }

        // Build a minimal JSON-RPC request body without Newtonsoft.Json
        private string BuildRpcJson(MCPRpcRequest req)
        {
            return $"{{\"jsonrpc\":\"{req.jsonrpc}\",\"id\":{req.id},\"method\":\"{req.method}\",\"params\":{req.paramsJson}}}";
        }

        // Very small helper to convert anonymous objects to JSON (only handles simple cases)
        private string SimpleJson(object obj)
        {
            if (obj == null) return "null";
            return JsonUtility.ToJson(obj);
        }

        // Extract the "result" field value from a JSON-RPC response string
        private string ExtractResultField(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var resultKey = "\"result\":";
            int idx = json.IndexOf(resultKey, StringComparison.Ordinal);
            if (idx < 0) return null;

            int start = idx + resultKey.Length;
            // Find the matching end of the result value (object, array, or primitive)
            int end = FindJsonValueEnd(json, start);
            return end > start ? json.Substring(start, end - start).Trim() : null;
        }

        private int FindJsonValueEnd(string json, int start)
        {
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
            if (start >= json.Length) return start;

            char first = json[start];
            if (first == '{' || first == '[')
            {
                // Find matching closing brace/bracket, handling escaped characters properly
                char open = first, close = first == '{' ? '}' : ']';
                int depth = 0;
                bool inString = false;
                for (int i = start; i < json.Length; i++)
                {
                    char c = json[i];
                    if (c == '"' && !IsEscaped(json, i)) inString = !inString;
                    if (!inString)
                    {
                        if (c == open) depth++;
                        else if (c == close) { depth--; if (depth == 0) return i + 1; }
                    }
                }
                return json.Length;
            }
            else
            {
                // Primitive value: read until , or } or ]
                int end = start;
                bool inStr = first == '"';
                if (inStr) end++; // skip opening quote
                while (end < json.Length)
                {
                    char c = json[end];
                    if (inStr)
                    {
                        if (c == '"' && !IsEscaped(json, end)) { end++; break; }
                    }
                    else if (c == ',' || c == '}' || c == ']')
                    {
                        break;
                    }
                    end++;
                }
                return end;
            }
        }

        // Returns true when the character at position i in json is preceded by an odd number
        // of backslashes (i.e. it is escaped), correctly handling sequences like \\\".
        private static bool IsEscaped(string json, int i)
        {
            int backslashCount = 0;
            int pos = i - 1;
            while (pos >= 0 && json[pos] == '\\')
            {
                backslashCount++;
                pos--;
            }
            return backslashCount % 2 != 0;
        }

        // ------------------------------------------------------------------ list parsers

        private List<MCPTool> ParseToolsList(string json, string serverName)
        {
            var tools = new List<MCPTool>();
            try
            {
                var wrapper = JsonUtility.FromJson<MCPToolsListResult>(WrapArray(json, "tools"));
                if (wrapper?.tools != null)
                {
                    foreach (var t in wrapper.tools)
                    {
                        t.ServerName = serverName;
                        t.Enabled = true;
                        tools.Add(t);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPClient] ParseToolsList error: {ex.Message}");
            }
            return tools;
        }

        private List<MCPResource> ParseResourcesList(string json, string serverName)
        {
            var resources = new List<MCPResource>();
            try
            {
                var wrapper = JsonUtility.FromJson<MCPResourcesListResult>(WrapArray(json, "resources"));
                if (wrapper?.resources != null)
                {
                    foreach (var r in wrapper.resources)
                    {
                        r.ServerName = serverName;
                        resources.Add(r);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPClient] ParseResourcesList error: {ex.Message}");
            }
            return resources;
        }

        private List<MCPPrompt> ParsePromptsList(string json, string serverName)
        {
            var prompts = new List<MCPPrompt>();
            try
            {
                var wrapper = JsonUtility.FromJson<MCPPromptsListResult>(WrapArray(json, "prompts"));
                if (wrapper?.prompts != null)
                {
                    foreach (var p in wrapper.prompts)
                    {
                        p.ServerName = serverName;
                        prompts.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPClient] ParsePromptsList error: {ex.Message}");
            }
            return prompts;
        }

        // If the JSON result is already a JSON object containing the array key,
        // use it directly; otherwise wrap it.
        private string WrapArray(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return $"{{\"{key}\":[]}}";
            json = json.Trim();
            if (json.StartsWith("{")) return json;           // already an object
            return $"{{\"{key}\":{json}}}";
        }

        // ------------------------------------------------------------------ misc helpers

        private MCPTool FindToolForTask(AgentTask task)
        {
            return availableTools.FirstOrDefault(t => t.Enabled && t.CanHandle(task));
        }

        private async Task<string> CallMCPTool(MCPTool tool, AgentTask task)
        {
            if (!connections.TryGetValue(tool.ServerName, out var connection))
                throw new Exception($"Server '{tool.ServerName}' not connected");

            var result = await SendRpcRequest(connection.Url, "tools/call",
                new { name = tool.Name, arguments = task.Parameters ?? new Dictionary<string, object>() });

            return result ?? string.Empty;
        }

        private void ApplyToolToggles()
        {
            if (config?.MCPToolToggles == null) return;

            foreach (var toggle in config.MCPToolToggles)
            {
                var tool = availableTools.FirstOrDefault(t => t.Name == toggle.ToolName);
                if (tool != null)
                    tool.Enabled = toggle.Enabled;
            }
        }
    }

    // ===================================================================== data model

    [Serializable]
    public class MCPServerConnection
    {
        public string Name;
        public string Url;
        public bool IsConnected;
    }

    [Serializable]
    public class MCPTool
    {
        public string Name;
        public string ServerName;
        public string Description;
        public bool Enabled = true;
        public List<string> ParameterNames = new List<string>();

        public bool CanHandle(AgentTask task)
        {
            var desc = task.Description.ToLower();
            return desc.Contains(Name.ToLower());
        }
    }

    [Serializable]
    public class MCPResource
    {
        public string Name;
        public string ServerName;
        public string Type;
        public string Uri;
        public string Description;
    }

    [Serializable]
    public class MCPPrompt
    {
        public string Name;
        public string ServerName;
        public string Description;
        public List<string> Parameters = new List<string>();
    }

    // ------------------------------------------------------------------ JSON-RPC helpers

    internal class MCPRpcRequest
    {
        public string jsonrpc;
        public int id;
        public string method;
        public string paramsJson;   // pre-serialised params object
    }

    [Serializable]
    internal class MCPToolsListResult
    {
        public List<MCPTool> tools;
    }

    [Serializable]
    internal class MCPResourcesListResult
    {
        public List<MCPResource> resources;
    }

    [Serializable]
    internal class MCPPromptsListResult
    {
        public List<MCPPrompt> prompts;
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Model Context Protocol (MCP) client for connecting to MCP servers
    /// </summary>
    public class MCPClient : MonoBehaviour
    {
        [SerializeField] private AIAgentConfig config;
        private Dictionary<string, MCPServerConnection> connections = new Dictionary<string, MCPServerConnection>();
        private List<MCPTool> availableTools = new List<MCPTool>();
        private List<MCPResource> availableResources = new List<MCPResource>();
        private List<MCPPrompt> availablePrompts = new List<MCPPrompt>();
        private static HttpClient httpClient;

        public bool IsConnected => connections.Count > 0;
        public List<MCPTool> AvailableTools => availableTools;
        public List<MCPResource> AvailableResources => availableResources;
        public List<MCPPrompt> AvailablePrompts => availablePrompts;

        static MCPClient()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        private void Start()
        {
            ConnectToServers();
        }

        /// <summary>
        /// Connect to all configured MCP servers
        /// </summary>
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
                {
                    await ConnectToServer(serverConfig);
                }
            }
        }

        /// <summary>
        /// Connect to a single MCP server
        /// </summary>
        private async Task ConnectToServer(MCPServerConfig serverConfig)
        {
            try
            {
                Debug.Log($"[MCPClient] Connecting to MCP server: {serverConfig.Name}");

                var connection = new MCPServerConnection
                {
                    Name = serverConfig.Name,
                    Url = serverConfig.Url,
                    IsConnected = false
                };

                // Initialize connection (in real implementation, this would establish WebSocket or HTTP connection)
                var initUrl = $"{serverConfig.Url}/initialize";
                var response = await httpClient.GetAsync(initUrl);

                if (response.IsSuccessStatusCode)
                {
                    connection.IsConnected = true;
                    connections[serverConfig.Name] = connection;

                    // Fetch available tools, resources, and prompts
                    await FetchServerCapabilities(connection);

                    Debug.Log($"[MCPClient] Connected to MCP server: {serverConfig.Name}");
                }
                else
                {
                    Debug.LogWarning($"[MCPClient] Failed to connect to MCP server: {serverConfig.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPClient] Error connecting to MCP server {serverConfig.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetch capabilities from MCP server
        /// </summary>
        private async Task FetchServerCapabilities(MCPServerConnection connection)
        {
            try
            {
                // Fetch tools
                var toolsUrl = $"{connection.Url}/tools";
                var toolsResponse = await httpClient.GetAsync(toolsUrl);
                if (toolsResponse.IsSuccessStatusCode)
                {
                    var toolsJson = await toolsResponse.Content.ReadAsStringAsync();
                    // Parse and add tools
                    Debug.Log($"[MCPClient] Fetched tools from {connection.Name}");
                }

                // Fetch resources
                var resourcesUrl = $"{connection.Url}/resources";
                var resourcesResponse = await httpClient.GetAsync(resourcesUrl);
                if (resourcesResponse.IsSuccessStatusCode)
                {
                    var resourcesJson = await resourcesResponse.Content.ReadAsStringAsync();
                    // Parse and add resources
                    Debug.Log($"[MCPClient] Fetched resources from {connection.Name}");
                }

                // Fetch prompts
                var promptsUrl = $"{connection.Url}/prompts";
                var promptsResponse = await httpClient.GetAsync(promptsUrl);
                if (promptsResponse.IsSuccessStatusCode)
                {
                    var promptsJson = await promptsResponse.Content.ReadAsStringAsync();
                    // Parse and add prompts
                    Debug.Log($"[MCPClient] Fetched prompts from {connection.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPClient] Error fetching capabilities: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a task using MCP tools
        /// </summary>
        public async Task<string> ExecuteTask(AgentTask task)
        {
            // Find appropriate MCP tool for the task
            var tool = FindToolForTask(task);
            
            if (tool != null)
            {
                return await CallMCPTool(tool, task);
            }

            return "No MCP tool found for task";
        }

        /// <summary>
        /// Find an MCP tool that can handle the task
        /// </summary>
        private MCPTool FindToolForTask(AgentTask task)
        {
            foreach (var tool in availableTools)
            {
                if (tool.CanHandle(task))
                {
                    return tool;
                }
            }
            return null;
        }

        /// <summary>
        /// Call an MCP tool
        /// </summary>
        private async Task<string> CallMCPTool(MCPTool tool, AgentTask task)
        {
            try
            {
                var connection = connections[tool.ServerName];
                var url = $"{connection.Url}/tools/{tool.Name}/execute";

                var requestBody = new
                {
                    parameters = task.Parameters
                };

                var json = JsonUtility.ToJson(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPClient] Error calling MCP tool: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get a resource from MCP server
        /// </summary>
        public async Task<string> GetResource(string resourceName)
        {
            var resource = availableResources.Find(r => r.Name == resourceName);
            if (resource == null)
            {
                throw new Exception($"Resource '{resourceName}' not found");
            }

            var connection = connections[resource.ServerName];
            var url = $"{connection.Url}/resources/{resourceName}";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Get a prompt from MCP server
        /// </summary>
        public async Task<string> GetPrompt(string promptName, Dictionary<string, string> parameters = null)
        {
            var prompt = availablePrompts.Find(p => p.Name == promptName);
            if (prompt == null)
            {
                throw new Exception($"Prompt '{promptName}' not found");
            }

            var connection = connections[prompt.ServerName];
            var url = $"{connection.Url}/prompts/{promptName}";

            if (parameters != null && parameters.Count > 0)
            {
                var queryParams = new List<string>();
                foreach (var param in parameters)
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");
                }
                url += "?" + string.Join("&", queryParams);
            }

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Disconnect from all servers
        /// </summary>
        public void DisconnectAll()
        {
            connections.Clear();
            availableTools.Clear();
            availableResources.Clear();
            availablePrompts.Clear();
            
            Debug.Log("[MCPClient] Disconnected from all MCP servers");
        }
    }

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
        public Dictionary<string, string> Parameters;

        public bool CanHandle(AgentTask task)
        {
            // Simple keyword matching
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
    }

    [Serializable]
    public class MCPPrompt
    {
        public string Name;
        public string ServerName;
        public string Description;
        public List<string> Parameters;
    }
}

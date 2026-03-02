using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Configuration for AI Agent
    /// </summary>
    [CreateAssetMenu(fileName = "AIAgentConfig", menuName = "Unity3DAgent/AI Agent Config")]
    public class AIAgentConfig : ScriptableObject
    {
        [Header("Model API Configuration")]
        public ModelApiConfig ModelApiConfig;
        
        [Header("Skills Configuration")]
        public List<string> SkillDirectories = new List<string>();
        public List<SkillToggle> SkillToggles = new List<SkillToggle>();
        
        [Header("MCP Configuration")]
        public List<MCPServerConfig> MCPServers = new List<MCPServerConfig>();
        
        [Header("Execution Settings")]
        public int MaxParallelTasks = 5;
        public float TaskTimeoutSeconds = 300f;
    }

    [Serializable]
    public class ModelApiConfig
    {
        public ModelProvider Provider;
        public string BaseUrl;
        public string ModelName;
        public string ApiKey;
        public float Temperature = 0.7f;
        public int MaxTokens = 2048;
    }

    public enum ModelProvider
    {
        Ollama,
        OpenAI,
        Anthropic,
        Custom
    }

    [Serializable]
    public class SkillToggle
    {
        public string SkillName;
        public bool Enabled;
    }

    [Serializable]
    public class MCPServerConfig
    {
        public string Name;
        public string Url;
        public bool Enabled;
        public Dictionary<string, string> Parameters;

        public MCPServerConfig()
        {
            Parameters = new Dictionary<string, string>();
        }
    }
}

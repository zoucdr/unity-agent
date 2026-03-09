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

        [Header("Dynamic Skill Loader")]
        public SkillLoaderConfig SkillLoaderConfig = new SkillLoaderConfig();

        [Header("Standard Skill Packages (Claude SKILL.md format)")]
        [Tooltip("Enable automatic discovery and loading of standard Claude SKILL packages.")]
        public bool StandardSkillsEnabled = true;
        [Tooltip("Root directories to search recursively for SKILL.md skill packages.")]
        public List<string> StandardSkillRootPaths = new List<string>();
        [Tooltip("Per-skill enable/disable toggles for standard skill packages.")]
        public List<SkillToggle> StandardSkillToggles = new List<SkillToggle>();

        [Header("MCP Configuration")]
        public List<MCPServerConfig> MCPServers = new List<MCPServerConfig>();
        public List<MCPToolToggle> MCPToolToggles = new List<MCPToolToggle>();

        [Header("RAG Configuration")]
        public RAGConfig RAGConfig = new RAGConfig();

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
        public bool Enabled = true;
    }

    /// <summary>
    /// Configures paths (folders and/or zip files) from which skills are loaded dynamically.
    /// </summary>
    [Serializable]
    public class SkillLoaderConfig
    {
        public bool Enabled = false;
        /// <summary>File-system folder paths to scan for skill DLLs.</summary>
        public List<string> FolderPaths = new List<string>();
        /// <summary>Zip package paths to extract and scan for skill DLLs.</summary>
        public List<string> ZipPaths = new List<string>();
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

    /// <summary>Runtime toggle for an individual MCP tool.</summary>
    [Serializable]
    public class MCPToolToggle
    {
        public string ToolName;
        public bool Enabled = true;
    }

    /// <summary>
    /// Configuration for the RAG (Retrieval-Augmented Generation) subsystem.
    /// </summary>
    [Serializable]
    public class RAGConfig
    {
        public bool Enabled = false;
        /// <summary>Automatically index documents when the agent starts.</summary>
        public bool AutoLoadDocuments = false;
        /// <summary>File or folder paths containing .md / .txt documents to index.</summary>
        public List<string> DocumentPaths = new List<string>();
        /// <summary>Number of document chunks to retrieve per query.</summary>
        public int TopKResults = 3;
        /// <summary>Maximum number of words per document chunk.</summary>
        public int ChunkSize = 500;
        /// <summary>Number of words to overlap between consecutive chunks.</summary>
        public int ChunkOverlap = 100;
    }
}

using UnityEngine;
using UnityEditor;
using Unity3DAgent.Core;
using System.Collections.Generic;
using System.Linq;

namespace Unity3DAgent.Editor
{
    /// <summary>
    /// Unity Editor window for AI Agent.
    /// Provides tabs for Tasks, Config, Skills, MCP, RAG, and Sub-Agents.
    /// All runtime toggles (skill enable/disable, MCP tool enable/disable)
    /// are exposed here and take effect immediately.
    /// </summary>
    public class AIAgentWindow : EditorWindow
    {
        private AIAgent agent;
        private string commandInput = "";
        private Vector2 scrollPosition;
        private Vector2 taskScrollPosition;

        // active tab
        private enum Tab { Tasks, Config, Skills, MCP, RAG, SubAgents }
        private Tab activeTab = Tab.Tasks;

        // skill loader UI state
        private string newFolderPath = "";
        private string newZipPath = "";

        // RAG UI state
        private string ragDocPath = "";

        // sub-agent UI state
        private string subAgentName = "";
        private string subAgentCommand = "";

        [MenuItem("Window/AI Agent/Control Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<AIAgentWindow>("AI Agent");
            window.minSize = new Vector2(450, 350);
        }

        private void OnEnable() => FindAgent();

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            DrawHeader();
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (activeTab)
            {
                case Tab.Tasks:     DrawTasksPanel();     break;
                case Tab.Config:    DrawConfigPanel();    break;
                case Tab.Skills:    DrawSkillsPanel();    break;
                case Tab.MCP:       DrawMCPPanel();       break;
                case Tab.RAG:       DrawRAGPanel();       break;
                case Tab.SubAgents: DrawSubAgentsPanel(); break;
            }

            EditorGUILayout.EndScrollView();
            DrawCommandInput();
            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------------ header

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("AI Agent Control Panel", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            });
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(50));

            if (agent == null)
            {
                EditorGUILayout.LabelField("No agent in scene", EditorStyles.miniLabel);
                if (GUILayout.Button("Create Agent", GUILayout.Width(100)))
                    CreateAgent();
            }
            else
            {
                var originalColor = GUI.color;
                GUI.color = agent.IsProcessing ? Color.green : Color.gray;
                EditorGUILayout.LabelField(agent.IsProcessing ? "Processing" : "Idle",
                    EditorStyles.boldLabel, GUILayout.Width(100));
                GUI.color = originalColor;

                if (agent.IsProcessing && GUILayout.Button("Cancel All", GUILayout.Width(100)))
                    agent.CancelAllTasks();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
            DrawSeparator();
        }

        // ------------------------------------------------------------------ toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            DrawTabButton(Tab.Tasks,     "Tasks");
            DrawTabButton(Tab.Config,    "Config");
            DrawTabButton(Tab.Skills,    "Skills");
            DrawTabButton(Tab.MCP,       "MCP");
            DrawTabButton(Tab.RAG,       "RAG");
            DrawTabButton(Tab.SubAgents, "Sub-Agents");

            EditorGUILayout.EndHorizontal();
            DrawSeparator();
        }

        private void DrawTabButton(Tab tab, string label)
        {
            if (GUILayout.Toggle(activeTab == tab, label, EditorStyles.toolbarButton))
                activeTab = tab;
        }

        // ------------------------------------------------------------------ Tasks panel

        private void DrawTasksPanel()
        {
            EditorGUILayout.LabelField("Task Graph", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (agent == null)
            {
                EditorGUILayout.HelpBox("No agent found. Create an agent to see tasks.", MessageType.Info);
                return;
            }

            if (agent.RunningTasks.Count > 0)
            {
                EditorGUILayout.LabelField($"Running Tasks ({agent.RunningTasks.Count}):", EditorStyles.boldLabel);
                taskScrollPosition = EditorGUILayout.BeginScrollView(taskScrollPosition, GUILayout.Height(150));
                foreach (var task in agent.RunningTasks)
                    DrawTask(task, Color.yellow);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(10);
            }

            if (agent.CompletedTasks.Count > 0)
            {
                EditorGUILayout.LabelField($"Completed Tasks ({agent.CompletedTasks.Count}):", EditorStyles.boldLabel);
                foreach (var task in agent.CompletedTasks.TakeLast(10))
                    DrawTask(task, task.Status == TaskStatus.Completed ? Color.green : Color.red);
            }
            else if (agent.RunningTasks.Count == 0)
            {
                EditorGUILayout.HelpBox("No tasks to display. Enter a command below to start.", MessageType.Info);
            }
        }

        private void DrawTask(AgentTask task, Color statusColor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField("●", GUILayout.Width(15));
            GUI.color = originalColor;

            EditorGUILayout.LabelField(task.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"[{task.Status}]", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(task.Description, EditorStyles.wordWrappedLabel);

            if (task.Status == TaskStatus.Running)
            {
                var rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, task.Progress, $"{(task.Progress * 100):F0}%");
            }

            if (!string.IsNullOrEmpty(task.Result))
            {
                EditorGUILayout.LabelField("Result:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(task.Result, EditorStyles.wordWrappedMiniLabel);
            }

            if (!string.IsNullOrEmpty(task.Error))
                EditorGUILayout.HelpBox(task.Error, MessageType.Error);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // ------------------------------------------------------------------ Config panel

        private void DrawConfigPanel()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (agent == null || agent.Config == null)
            {
                EditorGUILayout.HelpBox("No agent or config found.", MessageType.Warning);
                return;
            }

            var config = agent.Config;

            EditorGUILayout.LabelField("Model API Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (config.ModelApiConfig != null)
            {
                EditorGUILayout.LabelField($"Provider: {config.ModelApiConfig.Provider}");
                EditorGUILayout.LabelField($"Base URL: {config.ModelApiConfig.BaseUrl}");
                EditorGUILayout.LabelField($"Model: {config.ModelApiConfig.ModelName}");
                EditorGUILayout.LabelField($"Temperature: {config.ModelApiConfig.Temperature}");
            }
            else
            {
                EditorGUILayout.LabelField("Not configured");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Execution Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Max Parallel Tasks: {config.MaxParallelTasks}");
            EditorGUILayout.LabelField($"Task Timeout: {config.TaskTimeoutSeconds}s");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Edit Config Asset"))
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }

        // ------------------------------------------------------------------ Skills panel

        private void DrawSkillsPanel()
        {
            EditorGUILayout.LabelField("Skills Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var skillManager = agent?.GetComponent<SkillManager>();

            if (skillManager == null)
            {
                EditorGUILayout.HelpBox("No SkillManager found on agent.", MessageType.Warning);
                return;
            }

            // Runtime toggle per skill
            EditorGUILayout.LabelField($"Loaded Skills: {skillManager.LoadedSkills.Count}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (skillManager.LoadedSkills.Count == 0)
            {
                EditorGUILayout.HelpBox("No skills loaded. Add skill assets to Resources/Skills, or load from a folder/zip below.", MessageType.Info);
            }
            else
            {
                foreach (var skill in skillManager.LoadedSkills)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    bool newEnabled = EditorGUILayout.Toggle(skill.Enabled, GUILayout.Width(20));
                    if (newEnabled != skill.Enabled)
                        skillManager.SetSkillEnabled(skill.SkillName, newEnabled);

                    var originalColor = GUI.color;
                    GUI.color = skill.Enabled ? Color.green : Color.gray;
                    EditorGUILayout.LabelField("●", GUILayout.Width(15));
                    GUI.color = originalColor;

                    EditorGUILayout.LabelField(skill.SkillName, EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField(skill.Description, EditorStyles.wordWrappedLabel);
                    if (skill.Keywords.Count > 0)
                        EditorGUILayout.LabelField($"Keywords: {string.Join(", ", skill.Keywords)}", EditorStyles.miniLabel);

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.Space(10);

            // --- Dynamic loading ---
            EditorGUILayout.LabelField("Dynamic Skill Loading", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Load from folder (DLLs):", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            newFolderPath = EditorGUILayout.TextField(newFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Skill Folder", "", "");
                if (!string.IsNullOrEmpty(path)) newFolderPath = path;
            }
            if (GUILayout.Button("Load", GUILayout.Width(50)) && !string.IsNullOrEmpty(newFolderPath))
            {
                skillManager.LoadSkillsFromFolder(newFolderPath);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Load from zip package:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            newZipPath = EditorGUILayout.TextField(newZipPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("Select Skill Zip", "", "zip");
                if (!string.IsNullOrEmpty(path)) newZipPath = path;
            }
            if (GUILayout.Button("Load", GUILayout.Width(50)) && !string.IsNullOrEmpty(newZipPath))
            {
                skillManager.LoadSkillsFromZip(newZipPath);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Reload All Skills"))
            {
                skillManager.ReloadSkills();
                Repaint();
            }
        }

        // ------------------------------------------------------------------ MCP panel

        private void DrawMCPPanel()
        {
            EditorGUILayout.LabelField("MCP Client", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var mcpClient = agent?.GetComponent<MCPClient>();
            if (mcpClient == null)
            {
                EditorGUILayout.HelpBox("No MCPClient found on agent.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(
                $"Status: {(mcpClient.IsConnected ? "Connected" : "Disconnected")}",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Tools with runtime toggle
            EditorGUILayout.LabelField($"Tools ({mcpClient.AvailableTools.Count}):", EditorStyles.boldLabel);
            if (mcpClient.AvailableTools.Count == 0)
            {
                EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var tool in mcpClient.AvailableTools)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    bool newEnabled = EditorGUILayout.Toggle(tool.Enabled, GUILayout.Width(20));
                    if (newEnabled != tool.Enabled)
                        mcpClient.SetToolEnabled(tool.Name, newEnabled);

                    var originalColor = GUI.color;
                    GUI.color = tool.Enabled ? Color.green : Color.gray;
                    EditorGUILayout.LabelField("●", GUILayout.Width(15));
                    GUI.color = originalColor;

                    EditorGUILayout.LabelField(tool.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(tool.Description, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(5);

            // Resources
            EditorGUILayout.LabelField($"Resources ({mcpClient.AvailableResources.Count}):", EditorStyles.boldLabel);
            foreach (var res in mcpClient.AvailableResources)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(res.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"URI: {res.Uri}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Type: {res.Type}", EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(res.Description))
                    EditorGUILayout.LabelField(res.Description, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Prompts
            EditorGUILayout.LabelField($"Prompts ({mcpClient.AvailablePrompts.Count}):", EditorStyles.boldLabel);
            foreach (var prompt in mcpClient.AvailablePrompts)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(prompt.Name, EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(prompt.Description))
                    EditorGUILayout.LabelField(prompt.Description, EditorStyles.wordWrappedMiniLabel);
                if (prompt.Parameters.Count > 0)
                    EditorGUILayout.LabelField($"Params: {string.Join(", ", prompt.Parameters)}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reconnect"))
                mcpClient.ConnectToServers();
            if (GUILayout.Button("Refresh Capabilities"))
                mcpClient.RefreshCapabilities();
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ RAG panel

        private void DrawRAGPanel()
        {
            EditorGUILayout.LabelField("RAG – Knowledge Base", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var ragManager = agent?.GetComponent<RAGManager>();
            if (ragManager == null)
            {
                EditorGUILayout.HelpBox("No RAGManager found on agent. Add a RAGManager component to enable RAG.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"Indexed Chunks: {ragManager.ChunkCount}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Add document / folder:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            ragDocPath = EditorGUILayout.TextField(ragDocPath);
            if (GUILayout.Button("Browse File", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFilePanel("Select Document", "", "md,txt");
                if (!string.IsNullOrEmpty(path)) ragDocPath = path;
            }
            if (GUILayout.Button("Browse Folder", GUILayout.Width(90)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Document Folder", "", "");
                if (!string.IsNullOrEmpty(path)) ragDocPath = path;
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Index Path") && !string.IsNullOrEmpty(ragDocPath))
            {
                ragManager.LoadDocumentsFromPath(ragDocPath);
                Repaint();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load From Config"))
            {
                ragManager.LoadDocumentsFromConfig();
                Repaint();
            }
            if (GUILayout.Button("Clear Index"))
            {
                ragManager.Clear();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ Sub-Agents panel

        private void DrawSubAgentsPanel()
        {
            EditorGUILayout.LabelField("Sub-Agent Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var subAgentManager = agent?.GetComponent<SubAgentManager>();
            if (subAgentManager == null)
            {
                EditorGUILayout.HelpBox("No SubAgentManager found on agent. Add a SubAgentManager component to enable nested agent calls.", MessageType.Warning);
                return;
            }

            var names = subAgentManager.RegisteredAgentNames.ToList();
            EditorGUILayout.LabelField($"Registered Agents ({names.Count}):", EditorStyles.boldLabel);

            if (names.Count == 0)
            {
                EditorGUILayout.LabelField("  (none registered yet)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var name in names)
                    EditorGUILayout.LabelField($"  • {name}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(10);

            // Delegate command to a sub-agent
            EditorGUILayout.LabelField("Delegate Command to Sub-Agent:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Agent Name:", GUILayout.Width(90));
            subAgentName = EditorGUILayout.TextField(subAgentName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Command:", GUILayout.Width(90));
            subAgentCommand = EditorGUILayout.TextField(subAgentCommand);
            EditorGUILayout.EndHorizontal();

            GUI.enabled = !string.IsNullOrWhiteSpace(subAgentName) && !string.IsNullOrWhiteSpace(subAgentCommand);
            if (GUILayout.Button("Execute on Sub-Agent"))
                ExecuteOnSubAgent(subAgentManager, subAgentName, subAgentCommand);
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------------ command input

        private void DrawCommandInput()
        {
            DrawSeparator();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Command Input:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            commandInput = EditorGUILayout.TextField(commandInput, GUILayout.Height(40));

            GUI.enabled = agent != null && !string.IsNullOrWhiteSpace(commandInput);
            if (GUILayout.Button("Execute", GUILayout.Width(80), GUILayout.Height(40)))
                ExecuteCommand();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Enter a command for the AI Agent to execute. The agent will plan and execute tasks automatically.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------------ helpers

        private void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void FindAgent() => agent = FindObjectOfType<AIAgent>();

        private void CreateAgent()
        {
            var agentObj = AIAgentSetup.CreateAIAgentSystem();
            agent = agentObj.GetComponent<AIAgent>();
        }

        private async void ExecuteCommand()
        {
            if (agent == null || string.IsNullOrWhiteSpace(commandInput)) return;

            var command = commandInput;
            commandInput = "";

            Debug.Log($"[AIAgentWindow] Executing command: {command}");

            try
            {
                await agent.ExecuteCommand(command);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AIAgentWindow] Command execution failed: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Command execution failed: {ex.Message}", "OK");
            }
        }

        private async void ExecuteOnSubAgent(SubAgentManager subAgentManager, string targetName, string command)
        {
            try
            {
                await subAgentManager.ExecuteOnSubAgent(targetName, command);
                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AIAgentWindow] Sub-agent execution failed: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Sub-agent execution failed: {ex.Message}", "OK");
            }
        }

        private void Update()
        {
            if (agent != null && agent.IsProcessing)
                Repaint();
        }
    }
}

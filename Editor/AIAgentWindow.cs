using UnityEngine;
using UnityEditor;
using Unity3DAgent.Core;
using System.Collections.Generic;
using System.Linq;

namespace Unity3DAgent.Editor
{
    /// <summary>
    /// Unity Editor window for AI Agent
    /// </summary>
    public class AIAgentWindow : EditorWindow
    {
        private AIAgent agent;
        private string commandInput = "";
        private Vector2 scrollPosition;
        private Vector2 taskScrollPosition;
        private bool showTasks = true;
        private bool showConfig = false;
        private bool showSkills = false;
        private bool showMCP = false;

        [MenuItem("Window/AI Agent")]
        public static void ShowWindow()
        {
            var window = GetWindow<AIAgentWindow>("AI Agent");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            FindAgent();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawHeader();
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (showTasks)
            {
                DrawTasksPanel();
            }
            else if (showConfig)
            {
                DrawConfigPanel();
            }
            else if (showSkills)
            {
                DrawSkillsPanel();
            }
            else if (showMCP)
            {
                DrawMCPPanel();
            }

            EditorGUILayout.EndScrollView();

            DrawCommandInput();

            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("AI Agent Control Panel", titleStyle);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Agent status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(50));
            
            if (agent == null)
            {
                EditorGUILayout.LabelField("No agent in scene", EditorStyles.miniLabel);
                
                if (GUILayout.Button("Create Agent", GUILayout.Width(100)))
                {
                    CreateAgent();
                }
            }
            else
            {
                var statusColor = agent.IsProcessing ? Color.green : Color.gray;
                var statusText = agent.IsProcessing ? "Processing" : "Idle";
                
                var originalColor = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel, GUILayout.Width(100));
                GUI.color = originalColor;

                if (agent.IsProcessing && GUILayout.Button("Cancel All", GUILayout.Width(100)))
                {
                    agent.CancelAllTasks();
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawSeparator();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(showTasks, "Tasks", EditorStyles.toolbarButton))
            {
                showTasks = true;
                showConfig = false;
                showSkills = false;
                showMCP = false;
            }

            if (GUILayout.Toggle(showConfig, "Config", EditorStyles.toolbarButton))
            {
                showTasks = false;
                showConfig = true;
                showSkills = false;
                showMCP = false;
            }

            if (GUILayout.Toggle(showSkills, "Skills", EditorStyles.toolbarButton))
            {
                showTasks = false;
                showConfig = false;
                showSkills = true;
                showMCP = false;
            }

            if (GUILayout.Toggle(showMCP, "MCP", EditorStyles.toolbarButton))
            {
                showTasks = false;
                showConfig = false;
                showSkills = false;
                showMCP = true;
            }

            EditorGUILayout.EndHorizontal();
            
            DrawSeparator();
        }

        private void DrawTasksPanel()
        {
            EditorGUILayout.LabelField("Task Graph", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (agent == null)
            {
                EditorGUILayout.HelpBox("No agent found. Create an agent to see tasks.", MessageType.Info);
                return;
            }

            var runningTasks = agent.RunningTasks;
            var completedTasks = agent.CompletedTasks;

            // Running tasks
            if (runningTasks.Count > 0)
            {
                EditorGUILayout.LabelField($"Running Tasks ({runningTasks.Count}):", EditorStyles.boldLabel);
                
                taskScrollPosition = EditorGUILayout.BeginScrollView(taskScrollPosition, GUILayout.Height(150));
                
                foreach (var task in runningTasks)
                {
                    DrawTask(task, Color.yellow);
                }
                
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(10);
            }

            // Completed tasks
            if (completedTasks.Count > 0)
            {
                EditorGUILayout.LabelField($"Completed Tasks ({completedTasks.Count}):", EditorStyles.boldLabel);
                
                var recentTasks = completedTasks.TakeLast(10).ToList();
                
                foreach (var task in recentTasks)
                {
                    var color = task.Status == TaskStatus.Completed ? Color.green : Color.red;
                    DrawTask(task, color);
                }
            }
            else if (runningTasks.Count == 0)
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
            {
                EditorGUILayout.LabelField("Error:", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(task.Error, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

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

            var skills = skillManager.LoadedSkills;

            EditorGUILayout.LabelField($"Loaded Skills: {skills.Count}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (skills.Count == 0)
            {
                EditorGUILayout.HelpBox("No skills loaded. Add skills to Resources/Skills folder.", MessageType.Info);
            }
            else
            {
                foreach (var skill in skills)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    var enabledColor = skill.Enabled ? Color.green : Color.gray;
                    var originalColor = GUI.color;
                    GUI.color = enabledColor;
                    EditorGUILayout.LabelField("●", GUILayout.Width(15));
                    GUI.color = originalColor;
                    
                    EditorGUILayout.LabelField(skill.SkillName, EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField(skill.Description, EditorStyles.wordWrappedLabel);
                    
                    if (skill.Keywords.Count > 0)
                    {
                        EditorGUILayout.LabelField($"Keywords: {string.Join(", ", skill.Keywords)}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Reload Skills"))
            {
                skillManager.ReloadSkills();
                Repaint();
            }
        }

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

            EditorGUILayout.LabelField($"Status: {(mcpClient.IsConnected ? "Connected" : "Disconnected")}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"Available Tools: {mcpClient.AvailableTools.Count}");
            EditorGUILayout.LabelField($"Available Resources: {mcpClient.AvailableResources.Count}");
            EditorGUILayout.LabelField($"Available Prompts: {mcpClient.AvailablePrompts.Count}");

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Reconnect"))
            {
                mcpClient.ConnectToServers();
            }
        }

        private void DrawCommandInput()
        {
            DrawSeparator();
            
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("Command Input:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            commandInput = EditorGUILayout.TextField(commandInput, GUILayout.Height(40));
            
            var canExecute = agent != null && !string.IsNullOrWhiteSpace(commandInput);
            
            GUI.enabled = canExecute;
            if (GUILayout.Button("Execute", GUILayout.Width(80), GUILayout.Height(40)))
            {
                ExecuteCommand();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Enter a command for the AI Agent to execute. The agent will plan and execute tasks automatically.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void FindAgent()
        {
            agent = FindObjectOfType<AIAgent>();
        }

        private void CreateAgent()
        {
            var agentObj = new GameObject("AI Agent");
            agent = agentObj.AddComponent<AIAgent>();
            agentObj.AddComponent<SkillManager>();
            agentObj.AddComponent<SkillExecutor>();
            agentObj.AddComponent<MCPClient>();
            
            Selection.activeGameObject = agentObj;
            
            Debug.Log("[AIAgentWindow] Created new AI Agent in scene");
        }

        private async void ExecuteCommand()
        {
            if (agent == null || string.IsNullOrWhiteSpace(commandInput))
            {
                return;
            }

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

        private void Update()
        {
            // Repaint window regularly to show task updates
            if (agent != null && agent.IsProcessing)
            {
                Repaint();
            }
        }
    }
}

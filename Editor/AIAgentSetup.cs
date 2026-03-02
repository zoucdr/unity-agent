using UnityEngine;
using UnityEditor;
using Unity3DAgent.Core;

namespace Unity3DAgent.Editor
{
    /// <summary>
    /// Utility class for setting up AI Agent in scenes
    /// </summary>
    public static class AIAgentSetup
    {
        [MenuItem("GameObject/AI Agent/Create AI Agent System", false, 10)]
        public static GameObject CreateAIAgentSystem()
        {
            // Create the main agent object
            var agentObj = new GameObject("AI Agent System");
            
            // Add core components
            var agent = agentObj.AddComponent<AIAgent>();
            agentObj.AddComponent<SkillManager>();
            agentObj.AddComponent<SkillExecutor>();
            agentObj.AddComponent<MCPClient>();
            
            // Try to find or create default config
            var config = FindOrCreateDefaultConfig();
            if (config != null)
            {
                // Use reflection to set the config since it's SerializeField
                var configField = typeof(AIAgent).GetField("config", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (configField != null)
                {
                    configField.SetValue(agent, config);
                }
            }
            
            // Mark as dirty for Unity to save
            EditorUtility.SetDirty(agentObj);
            
            // Select the created object
            Selection.activeGameObject = agentObj;
            
            Debug.Log("[AIAgentSetup] Created AI Agent System in scene");
            
            return agentObj;
        }

        [MenuItem("Assets/Create/Unity3DAgent/Default Agent Config", false, 80)]
        public static void CreateDefaultConfig()
        {
            var config = ScriptableObject.CreateInstance<AIAgentConfig>();
            
            // Set default values
            config.ModelApiConfig = new ModelApiConfig
            {
                Provider = ModelProvider.Ollama,
                BaseUrl = "http://localhost:11434",
                ModelName = "llama2",
                Temperature = 0.7f,
                MaxTokens = 2048
            };
            
            config.MaxParallelTasks = 5;
            config.TaskTimeoutSeconds = 300f;
            
            // Save the asset
            string path = "Assets/DefaultAIAgentConfig.asset";
            
            // Find unique name if file exists
            int counter = 1;
            while (System.IO.File.Exists(path))
            {
                path = $"Assets/DefaultAIAgentConfig_{counter}.asset";
                counter++;
            }
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created asset
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"[AIAgentSetup] Created default config at {path}");
        }

        private static AIAgentConfig FindOrCreateDefaultConfig()
        {
            // Try to find existing config in Assets
            var guids = AssetDatabase.FindAssets("t:AIAgentConfig");
            
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AIAgentConfig>(path);
            }
            
            // Create new config if none found
            CreateDefaultConfig();
            
            // Try to find it again
            guids = AssetDatabase.FindAssets("t:AIAgentConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AIAgentConfig>(path);
            }
            
            return null;
        }

        [MenuItem("GameObject/AI Agent/Create Example Skills", false, 11)]
        public static void CreateExampleSkills()
        {
            // Create Resources/Skills folder if it doesn't exist
            string skillsPath = "Assets/Resources/Skills";
            
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            if (!AssetDatabase.IsValidFolder(skillsPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Skills");
            }
            
            // Create example skills
            CreateExampleSkill<FileOperationSkill>(skillsPath, "FileOperationSkill");
            CreateExampleSkill<SceneOperationSkill>(skillsPath, "SceneOperationSkill");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[AIAgentSetup] Created example skills in Resources/Skills");
        }

        private static void CreateExampleSkill<T>(string path, string name) where T : Skill
        {
            var skill = ScriptableObject.CreateInstance<T>();
            skill.SkillName = name;
            skill.Enabled = true;
            
            string fullPath = $"{path}/{name}.asset";
            
            // Skip if already exists
            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log($"[AIAgentSetup] Skill {name} already exists, skipping");
                return;
            }
            
            AssetDatabase.CreateAsset(skill, fullPath);
            Debug.Log($"[AIAgentSetup] Created skill: {name}");
        }

        [MenuItem("Window/AI Agent/Open Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/zoucdr/unity3d-agent/blob/main/README.md");
        }

        [MenuItem("Window/AI Agent/Report Issue")]
        public static void ReportIssue()
        {
            Application.OpenURL("https://github.com/zoucdr/unity3d-agent/issues/new");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Manages and executes skills
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        [SerializeField] private AIAgentConfig config;
        private List<Skill> loadedSkills = new List<Skill>();
        private Dictionary<string, Skill> skillCache = new Dictionary<string, Skill>();

        public List<Skill> LoadedSkills => loadedSkills;

        private void Awake()
        {
            LoadSkills();
        }

        /// <summary>
        /// Load skills from configured directories
        /// </summary>
        public void LoadSkills()
        {
            loadedSkills.Clear();
            skillCache.Clear();

            if (config == null)
            {
                Debug.LogWarning("[SkillManager] No config set");
                return;
            }

            // Load skills from Resources folder
            var allSkills = Resources.LoadAll<Skill>("Skills");
            
            foreach (var skill in allSkills)
            {
                // Check if skill is enabled in config
                var toggle = config.SkillToggles.FirstOrDefault(t => t.SkillName == skill.SkillName);
                if (toggle != null && !toggle.Enabled)
                {
                    Debug.Log($"[SkillManager] Skill '{skill.SkillName}' is disabled");
                    continue;
                }

                loadedSkills.Add(skill);
                skillCache[skill.SkillName] = skill;
                
                Debug.Log($"[SkillManager] Loaded skill: {skill.SkillName}");
            }

            Debug.Log($"[SkillManager] Loaded {loadedSkills.Count} skills");
        }

        /// <summary>
        /// Find a skill that can handle the given task
        /// </summary>
        public Skill FindSkillForTask(AgentTask task, string context = "")
        {
            foreach (var skill in loadedSkills)
            {
                if (skill.CanHandle(task, context))
                {
                    Debug.Log($"[SkillManager] Found skill '{skill.SkillName}' for task '{task.Name}'");
                    return skill;
                }
            }

            Debug.LogWarning($"[SkillManager] No skill found for task '{task.Name}'");
            return null;
        }

        /// <summary>
        /// Get all skill capability descriptions for LLM
        /// </summary>
        public string GetAllSkillDescriptions()
        {
            var descriptions = new List<string>();
            
            foreach (var skill in loadedSkills)
            {
                if (skill.Enabled)
                {
                    descriptions.Add(skill.GetCapabilityDescription());
                }
            }

            return string.Join("\n", descriptions);
        }

        /// <summary>
        /// Reload skills
        /// </summary>
        public void ReloadSkills()
        {
            LoadSkills();
        }
    }

    /// <summary>
    /// Executes tasks using appropriate skills
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        private SkillManager skillManager;
        private AIAgent agent;

        private void Awake()
        {
            skillManager = GetComponent<SkillManager>();
            agent = GetComponent<AIAgent>();
        }

        /// <summary>
        /// Execute a task using the appropriate skill
        /// </summary>
        public async Task<string> ExecuteTask(AgentTask task)
        {
            var skill = skillManager.FindSkillForTask(task);
            
            if (skill != null)
            {
                try
                {
                    var result = await skill.Execute(task, task.Parameters);
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SkillExecutor] Skill execution failed: {ex.Message}");
                    throw;
                }
            }
            else
            {
                // No skill found, try to execute with MCP tools
                var mcpClient = GetComponent<MCPClient>();
                if (mcpClient != null && mcpClient.IsConnected)
                {
                    return await mcpClient.ExecuteTask(task);
                }
                
                // Fallback: Return task description
                return $"No handler found for task: {task.Description}";
            }
        }
    }
}

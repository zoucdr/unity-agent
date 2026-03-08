using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Manages and executes skills. Supports loading from Unity Resources as well as
    /// dynamically from file-system folders or zip packages via <see cref="SkillLoader"/>.
    /// Runtime enable/disable of individual skills is supported without reloading.
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

        // ------------------------------------------------------------------ loading

        /// <summary>
        /// Reload all skills: first from Unity Resources, then from any configured
        /// dynamic paths (folders and zip packages).
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

            // Load skills from the Resources/Skills folder (Unity built-in path)
            LoadFromResources();

            // Dynamic loading from file-system folders / zip files
            if (config.SkillLoaderConfig != null && config.SkillLoaderConfig.Enabled)
            {
                LoadFromDynamicPaths();
            }

            Debug.Log($"[SkillManager] Loaded {loadedSkills.Count} skill(s) total");
        }

        /// <summary>
        /// Load skills from a file-system folder at runtime without a full reload.
        /// </summary>
        public void LoadSkillsFromFolder(string folderPath)
        {
            var types = SkillLoader.LoadSkillTypesFromFolder(folderPath);
            var skills = SkillLoader.InstantiateSkills(types);
            RegisterSkills(skills);
        }

        /// <summary>
        /// Load skills from a zip package at runtime without a full reload.
        /// </summary>
        public void LoadSkillsFromZip(string zipPath)
        {
            var types = SkillLoader.LoadSkillTypesFromZip(zipPath);
            var skills = SkillLoader.InstantiateSkills(types);
            RegisterSkills(skills);
        }

        /// <summary>Reload all skills.</summary>
        public void ReloadSkills() => LoadSkills();

        // ------------------------------------------------------------------ runtime toggles

        /// <summary>
        /// Enable or disable a skill by name at runtime.
        /// Also updates the corresponding entry in the config's <see cref="AIAgentConfig.SkillToggles"/>.
        /// </summary>
        public void SetSkillEnabled(string skillName, bool enabled)
        {
            if (skillCache.TryGetValue(skillName, out var skill))
            {
                skill.Enabled = enabled;
            }

            if (config != null)
            {
                var toggle = config.SkillToggles.FirstOrDefault(t => t.SkillName == skillName);
                if (toggle != null)
                {
                    toggle.Enabled = enabled;
                }
                else
                {
                    config.SkillToggles.Add(new SkillToggle { SkillName = skillName, Enabled = enabled });
                }
            }

            Debug.Log($"[SkillManager] Skill '{skillName}' set enabled={enabled}");
        }

        /// <summary>Returns whether the named skill is currently enabled.</summary>
        public bool IsSkillEnabled(string skillName)
        {
            if (skillCache.TryGetValue(skillName, out var skill))
                return skill.Enabled;

            return false;
        }

        // ------------------------------------------------------------------ task matching

        /// <summary>Find the first enabled skill that can handle <paramref name="task"/>.</summary>
        public Skill FindSkillForTask(AgentTask task, string context = "")
        {
            foreach (var skill in loadedSkills)
            {
                if (skill.CanHandle(task, context))
                {
                    Debug.Log($"[SkillManager] Matched skill '{skill.SkillName}' for task '{task.Name}'");
                    return skill;
                }
            }

            Debug.LogWarning($"[SkillManager] No skill found for task '{task.Name}'");
            return null;
        }

        /// <summary>
        /// Return a newline-joined string of all enabled skill capability descriptions,
        /// suitable for inclusion in an LLM prompt.
        /// </summary>
        public string GetAllSkillDescriptions()
        {
            return string.Join("\n", loadedSkills.Where(s => s.Enabled).Select(s => s.GetCapabilityDescription()));
        }

        // ------------------------------------------------------------------ private helpers

        private void LoadFromResources()
        {
            var allSkills = Resources.LoadAll<Skill>("Skills");

            foreach (var skill in allSkills)
            {
                RegisterSkill(skill);
            }
        }

        private void LoadFromDynamicPaths()
        {
            var loaderCfg = config.SkillLoaderConfig;

            foreach (var folder in loaderCfg.FolderPaths)
            {
                if (!string.IsNullOrEmpty(folder))
                    LoadSkillsFromFolder(folder);
            }

            foreach (var zip in loaderCfg.ZipPaths)
            {
                if (!string.IsNullOrEmpty(zip))
                    LoadSkillsFromZip(zip);
            }
        }

        private void RegisterSkills(List<Skill> skills)
        {
            foreach (var skill in skills)
                RegisterSkill(skill);
        }

        private void RegisterSkill(Skill skill)
        {
            // Check config toggles
            if (config != null)
            {
                var toggle = config.SkillToggles.FirstOrDefault(t => t.SkillName == skill.SkillName);
                if (toggle != null)
                    skill.Enabled = toggle.Enabled;
            }

            if (skillCache.ContainsKey(skill.SkillName))
            {
                Debug.Log($"[SkillManager] Skill '{skill.SkillName}' already registered; skipping duplicate.");
                return;
            }

            loadedSkills.Add(skill);
            skillCache[skill.SkillName] = skill;
            Debug.Log($"[SkillManager] Registered skill: {skill.SkillName}");
        }
    }

    /// <summary>
    /// Executes tasks by selecting the most appropriate skill or falling back to MCP tools.
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

        /// <summary>Execute a task using the appropriate skill or MCP tool.</summary>
        public async Task<string> ExecuteTask(AgentTask task)
        {
            var skill = skillManager?.FindSkillForTask(task);

            if (skill != null)
            {
                try
                {
                    return await skill.Execute(task, task.Parameters);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SkillExecutor] Skill execution failed: {ex.Message}");
                    throw;
                }
            }

            // Fallback: MCP tools
            var mcpClient = GetComponent<MCPClient>();
            if (mcpClient != null && mcpClient.IsConnected)
            {
                try
                {
                    return await mcpClient.ExecuteTask(task);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SkillExecutor] MCP execution failed: {ex.Message}");
                    throw;
                }
            }

            throw new Exception($"No handler (skill or MCP tool) found for task: {task.Description}");
        }
    }
}

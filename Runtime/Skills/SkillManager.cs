using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Manages and executes skills.  Supports three loading strategies:
    /// <list type="number">
    ///   <item>Unity Resources (legacy, backward-compatible)</item>
    ///   <item>Dynamic DLL/Zip loading via <see cref="SkillLoader"/></item>
    ///   <item>Standard Claude SKILL packages (directories containing SKILL.md) via <see cref="SkillPackageLoader"/></item>
    /// </list>
    /// Runtime enable/disable of individual skills is supported without reloading.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        [SerializeField] private AIAgentConfig config;

        // Legacy Skill (ScriptableObject-based) state
        private List<Skill> loadedSkills = new List<Skill>();
        private Dictionary<string, Skill> skillCache = new Dictionary<string, Skill>();

        // Standard SKILL package state
        private List<SkillPackage> standardSkillPackages = new List<SkillPackage>();
        private Dictionary<string, SkillPackage> packageCache = new Dictionary<string, SkillPackage>();

        public List<Skill> LoadedSkills => loadedSkills;

        /// <summary>All standard Claude SKILL packages that have been discovered and registered.</summary>
        public List<SkillPackage> StandardSkillPackages => standardSkillPackages;

        private void Awake()
        {
            LoadSkills();
        }

        // ------------------------------------------------------------------ loading

        /// <summary>
        /// Reload all skills: Resources, dynamic DLL/Zip paths, and standard SKILL packages.
        /// </summary>
        public void LoadSkills()
        {
            loadedSkills.Clear();
            skillCache.Clear();
            standardSkillPackages.Clear();
            packageCache.Clear();

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

            // Standard Claude SKILL package discovery
            if (config.StandardSkillsEnabled)
            {
                LoadStandardSkillPackages();
            }

            Debug.Log($"[SkillManager] Loaded {loadedSkills.Count} legacy skill(s) and {standardSkillPackages.Count} standard skill package(s)");
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

        /// <summary>
        /// Discover and register standard SKILL packages from an additional root directory
        /// at runtime without a full reload.
        /// </summary>
        public void LoadStandardSkillPackagesFromPath(string rootPath)
        {
            var packages = SkillPackageLoader.DiscoverSkillPackages(rootPath);
            foreach (var pkg in packages)
                RegisterStandardSkillPackage(pkg);
        }

        /// <summary>Reload all skills and standard skill packages.</summary>
        public void ReloadSkills() => LoadSkills();

        // ------------------------------------------------------------------ runtime toggles (legacy)

        /// <summary>
        /// Enable or disable a legacy skill by name at runtime.
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

        /// <summary>Returns whether the named legacy skill is currently enabled.</summary>
        public bool IsSkillEnabled(string skillName)
        {
            if (skillCache.TryGetValue(skillName, out var skill))
                return skill.Enabled;

            return false;
        }

        // ------------------------------------------------------------------ runtime toggles (standard packages)

        /// <summary>
        /// Enable or disable a standard SKILL package by name at runtime.
        /// Also updates the corresponding entry in the config's <see cref="AIAgentConfig.StandardSkillToggles"/>.
        /// </summary>
        public void SetStandardSkillPackageEnabled(string packageName, bool enabled)
        {
            if (packageCache.TryGetValue(packageName, out var pkg))
            {
                pkg.Enabled = enabled;
            }

            if (config != null)
            {
                var toggle = config.StandardSkillToggles.FirstOrDefault(t => t.SkillName == packageName);
                if (toggle != null)
                {
                    toggle.Enabled = enabled;
                }
                else
                {
                    config.StandardSkillToggles.Add(new SkillToggle { SkillName = packageName, Enabled = enabled });
                }
            }

            Debug.Log($"[SkillManager] Standard skill package '{packageName}' set enabled={enabled}");
        }

        /// <summary>Returns whether the named standard skill package is currently enabled.</summary>
        public bool IsStandardSkillPackageEnabled(string packageName)
        {
            if (packageCache.TryGetValue(packageName, out var pkg))
                return pkg.Enabled;

            return false;
        }

        // ------------------------------------------------------------------ task matching

        /// <summary>Find the first enabled legacy skill that can handle <paramref name="task"/>.</summary>
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

            Debug.LogWarning($"[SkillManager] No legacy skill found for task '{task.Name}'");
            return null;
        }

        /// <summary>
        /// Find the first enabled standard SKILL package whose name appears as a whole word
        /// in the task description, or whose description is a whole-word substring.
        /// </summary>
        public SkillPackage FindStandardSkillForTask(AgentTask task, string context = "")
        {
            var desc = task.Description?.ToLowerInvariant() ?? string.Empty;

            foreach (var pkg in standardSkillPackages)
            {
                if (!pkg.Enabled)
                    continue;

                // Use whole-word matching to avoid false positives (e.g. "store" inside "restore")
                if (!string.IsNullOrEmpty(pkg.Name) && ContainsWholeWord(desc, pkg.Name.ToLowerInvariant()))
                {
                    Debug.Log($"[SkillManager] Matched standard skill package '{pkg.Name}' for task '{task.Name}'");
                    return pkg;
                }

                if (!string.IsNullOrEmpty(pkg.Description) && ContainsWholeWord(desc, pkg.Description.ToLowerInvariant()))
                {
                    Debug.Log($"[SkillManager] Matched standard skill package '{pkg.Name}' for task '{task.Name}'");
                    return pkg;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if <paramref name="text"/> contains <paramref name="word"/> as a
        /// whole word (surrounded by non-letter/digit characters or at the string boundary).
        /// </summary>
        private static bool ContainsWholeWord(string text, string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;

            var idx = text.IndexOf(word, StringComparison.Ordinal);
            while (idx >= 0)
            {
                bool leftOk  = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
                bool rightOk = idx + word.Length >= text.Length || !char.IsLetterOrDigit(text[idx + word.Length]);
                if (leftOk && rightOk)
                    return true;
                idx = text.IndexOf(word, idx + 1, StringComparison.Ordinal);
            }
            return false;
        }

        /// <summary>
        /// Return a newline-joined string of all enabled skill capability descriptions
        /// (both legacy and standard packages), suitable for inclusion in an LLM prompt.
        /// </summary>
        public string GetAllSkillDescriptions()
        {
            var descriptions = loadedSkills
                .Where(s => s.Enabled)
                .Select(s => s.GetCapabilityDescription())
                .Concat(
                    standardSkillPackages
                        .Where(p => p.Enabled)
                        .Select(p => p.GetCapabilityDescription())
                );
            return string.Join("\n", descriptions);
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

        private void LoadStandardSkillPackages()
        {
            foreach (var rootPath in config.StandardSkillRootPaths)
            {
                if (!string.IsNullOrEmpty(rootPath))
                    LoadStandardSkillPackagesFromPath(rootPath);
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

        private void RegisterStandardSkillPackage(SkillPackage pkg)
        {
            // Apply config toggle if present
            if (config != null)
            {
                var toggle = config.StandardSkillToggles.FirstOrDefault(t => t.SkillName == pkg.Name);
                if (toggle != null)
                    pkg.Enabled = toggle.Enabled;
            }

            if (packageCache.ContainsKey(pkg.Name))
            {
                Debug.Log($"[SkillManager] Standard skill package '{pkg.Name}' already registered; skipping duplicate.");
                return;
            }

            standardSkillPackages.Add(pkg);
            packageCache[pkg.Name] = pkg;
            Debug.Log($"[SkillManager] Registered standard skill package: {pkg.Name}");
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

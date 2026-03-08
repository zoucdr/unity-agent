using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Loads Skill implementations dynamically from file-system folders or zip packages.
    /// Scans .dll files inside those locations for types that derive from <see cref="Skill"/>
    /// and instantiates them as ScriptableObject instances ready for use by SkillManager.
    /// </summary>
    public static class SkillLoader
    {
        /// <summary>
        /// Load all skill types found in DLL files inside <paramref name="folderPath"/>.
        /// </summary>
        public static List<Type> LoadSkillTypesFromFolder(string folderPath)
        {
            var skillTypes = new List<Type>();

            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"[SkillLoader] Folder not found: {folderPath}");
                return skillTypes;
            }

            var dlls = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);
            foreach (var dll in dlls)
            {
                skillTypes.AddRange(LoadSkillTypesFromAssembly(dll));
            }

            Debug.Log($"[SkillLoader] Found {skillTypes.Count} skill type(s) in folder: {folderPath}");
            return skillTypes;
        }

        /// <summary>
        /// Extract a zip package to a temporary directory and load skill types from DLLs inside it.
        /// </summary>
        public static List<Type> LoadSkillTypesFromZip(string zipPath)
        {
            var skillTypes = new List<Type>();

            if (!File.Exists(zipPath))
            {
                Debug.LogWarning($"[SkillLoader] Zip file not found: {zipPath}");
                return skillTypes;
            }

            var tempDir = Path.Combine(
                Path.GetTempPath(),
                "unity_agent_skills_" + Path.GetFileNameWithoutExtension(zipPath));

            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                ZipFile.ExtractToDirectory(zipPath, tempDir);
                skillTypes.AddRange(LoadSkillTypesFromFolder(tempDir));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillLoader] Error extracting skills from zip '{zipPath}': {ex.Message}");
            }

            return skillTypes;
        }

        /// <summary>
        /// Instantiate ScriptableObject skill instances from a list of skill types.
        /// </summary>
        public static List<Skill> InstantiateSkills(List<Type> skillTypes)
        {
            var skills = new List<Skill>();

            foreach (var type in skillTypes)
            {
                try
                {
                    var skill = ScriptableObject.CreateInstance(type) as Skill;
                    if (skill != null)
                    {
                        skills.Add(skill);
                        Debug.Log($"[SkillLoader] Instantiated skill: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SkillLoader] Error instantiating skill '{type.Name}': {ex.Message}");
                }
            }

            return skills;
        }

        // ------------------------------------------------------------------ helpers

        private static List<Type> LoadSkillTypesFromAssembly(string assemblyPath)
        {
            var skillTypes = new List<Type>();

            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                var baseType = typeof(Skill);

                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsAbstract && baseType.IsAssignableFrom(type))
                    {
                        skillTypes.Add(type);
                        Debug.Log($"[SkillLoader] Discovered skill type: {type.FullName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillLoader] Error loading assembly '{assemblyPath}': {ex.Message}");
            }

            return skillTypes;
        }
    }
}

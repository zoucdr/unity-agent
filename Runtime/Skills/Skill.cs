using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Base class for AI Agent skills
    /// </summary>
    public abstract class Skill : ScriptableObject
    {
        [Header("Skill Information")]
        public string SkillName;
        public string Description;
        public List<string> Keywords = new List<string>();
        public bool Enabled = true;

        /// <summary>
        /// Check if this skill can handle the given task
        /// </summary>
        public abstract bool CanHandle(AgentTask task, string context);

        /// <summary>
        /// Execute the skill
        /// </summary>
        public abstract Task<string> Execute(AgentTask task, Dictionary<string, object> parameters);

        /// <summary>
        /// Get the skill's capability description for the LLM
        /// </summary>
        public virtual string GetCapabilityDescription()
        {
            return $"{SkillName}: {Description}";
        }
    }

    /// <summary>
    /// Example skill for file operations
    /// </summary>
    [CreateAssetMenu(fileName = "FileOperationSkill", menuName = "Unity3DAgent/Skills/File Operation")]
    public class FileOperationSkill : Skill
    {
        public override bool CanHandle(AgentTask task, string context)
        {
            if (!Enabled) return false;

            var desc = task.Description.ToLower();
            return desc.Contains("file") || desc.Contains("read") || 
                   desc.Contains("write") || desc.Contains("save");
        }

        public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
        {
            Debug.Log($"[FileOperationSkill] Executing: {task.Description}");
            
            // Simulate file operation
            await Task.Delay(500);
            
            return "File operation completed successfully";
        }
    }

    /// <summary>
    /// Example skill for Unity scene operations
    /// </summary>
    [CreateAssetMenu(fileName = "SceneOperationSkill", menuName = "Unity3DAgent/Skills/Scene Operation")]
    public class SceneOperationSkill : Skill
    {
        public override bool CanHandle(AgentTask task, string context)
        {
            if (!Enabled) return false;

            var desc = task.Description.ToLower();
            return desc.Contains("scene") || desc.Contains("gameobject") || 
                   desc.Contains("create") || desc.Contains("object");
        }

        public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
        {
            Debug.Log($"[SceneOperationSkill] Executing: {task.Description}");
            
            // Simulate scene operation
            await Task.Delay(500);
            
            return "Scene operation completed successfully";
        }
    }
}

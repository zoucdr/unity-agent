using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Represents a task to be executed by the AI Agent
    /// </summary>
    [Serializable]
    public class AgentTask
    {
        public string Id;
        public string Name;
        public string Description;
        public TaskStatus Status;
        public TaskExecutionMode ExecutionMode;
        public List<string> Dependencies;
        public Dictionary<string, object> Parameters;
        public float Progress;
        public string Result;
        public string Error;

        public AgentTask()
        {
            Id = Guid.NewGuid().ToString();
            Status = TaskStatus.Pending;
            Dependencies = new List<string>();
            Parameters = new Dictionary<string, object>();
            Progress = 0f;
        }
    }

    public enum TaskStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public enum TaskExecutionMode
    {
        Serial,
        Parallel
    }
}

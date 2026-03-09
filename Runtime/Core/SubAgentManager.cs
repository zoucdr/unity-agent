using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Manages a registry of named <see cref="AIAgent"/> instances and supports
    /// nested (sub-agent) task delegation: an agent can hand off tasks to any
    /// other registered agent and await the result.
    /// </summary>
    public class SubAgentManager : MonoBehaviour
    {
        private readonly Dictionary<string, AIAgent> registry = new Dictionary<string, AIAgent>(StringComparer.OrdinalIgnoreCase);

        // ------------------------------------------------------------------ registration

        /// <summary>Register an agent under a unique <paramref name="agentName"/>.</summary>
        public void RegisterAgent(string agentName, AIAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            if (string.IsNullOrEmpty(agentName))
                throw new ArgumentException("Agent name must not be empty.", nameof(agentName));

            registry[agentName] = agent;
            Debug.Log($"[SubAgentManager] Registered agent: {agentName}");
        }

        /// <summary>Remove an agent from the registry.</summary>
        public void UnregisterAgent(string agentName)
        {
            if (registry.Remove(agentName))
                Debug.Log($"[SubAgentManager] Unregistered agent: {agentName}");
        }

        /// <summary>Retrieve a registered agent by name, or <c>null</c> if not found.</summary>
        public AIAgent GetAgent(string agentName)
        {
            registry.TryGetValue(agentName, out var agent);
            return agent;
        }

        /// <summary>Returns all currently registered agent names.</summary>
        public IEnumerable<string> RegisteredAgentNames => registry.Keys;

        // ------------------------------------------------------------------ execution

        /// <summary>
        /// Delegate a single <paramref name="task"/> to the agent named
        /// <paramref name="targetAgentName"/> and wait for it to complete.
        /// Returns the task result string on success, or throws on failure.
        /// </summary>
        public async Task<string> DelegateTask(string targetAgentName, AgentTask task)
        {
            var agent = GetAgent(targetAgentName);
            if (agent == null)
                throw new Exception($"Sub-agent '{targetAgentName}' not found in registry.");

            Debug.Log($"[SubAgentManager] Delegating task '{task.Name}' to agent '{targetAgentName}'");

            // Execute the single task description as a command on the target agent
            var tasks = await agent.ExecuteCommand(task.Description);

            // Wait until the target agent finishes all its tasks
            await WaitForAgentIdle(agent);

            // Return aggregated results from completed tasks
            var results = new List<string>();
            foreach (var t in agent.CompletedTasks)
            {
                if (!string.IsNullOrEmpty(t.Result))
                    results.Add(t.Result);
            }

            var combined = string.Join("\n", results);
            Debug.Log($"[SubAgentManager] Sub-agent '{targetAgentName}' completed task '{task.Name}'");
            return combined;
        }

        /// <summary>
        /// Execute <paramref name="command"/> on the named sub-agent and return
        /// the list of planned <see cref="AgentTask"/> objects.
        /// </summary>
        public async Task<List<AgentTask>> ExecuteOnSubAgent(string targetAgentName, string command)
        {
            var agent = GetAgent(targetAgentName);
            if (agent == null)
                throw new Exception($"Sub-agent '{targetAgentName}' not found in registry.");

            Debug.Log($"[SubAgentManager] Executing command on sub-agent '{targetAgentName}': {command}");
            return await agent.ExecuteCommand(command);
        }

        // ------------------------------------------------------------------ helpers

        private async Task WaitForAgentIdle(AIAgent agent, float timeoutSeconds = 60f)
        {
            var startTime = DateTime.UtcNow;
            while (agent.IsProcessing)
            {
                if ((DateTime.UtcNow - startTime).TotalSeconds >= timeoutSeconds)
                {
                    Debug.LogWarning("[SubAgentManager] Timed out waiting for sub-agent to become idle.");
                    break;
                }

                await Task.Delay(100);
            }
        }
    }
}

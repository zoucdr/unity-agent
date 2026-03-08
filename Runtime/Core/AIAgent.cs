using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Core AI Agent controller.
    /// Accepts natural-language commands, uses an LLM to plan a task list,
    /// then executes each task via the skill/MCP stack.
    ///
    /// Sub-agent delegation is supported via <see cref="SubAgentManager"/>.
    /// When a <see cref="RAGManager"/> is present on the same GameObject its
    /// retrieved context is automatically prepended to the LLM planning prompt.
    /// </summary>
    public class AIAgent : MonoBehaviour
    {
        [SerializeField] private AIAgentConfig config;
        [SerializeField] private string agentName = "DefaultAgent";

        private Queue<AgentTask> taskQueue = new Queue<AgentTask>();
        private List<AgentTask> runningTasks = new List<AgentTask>();
        private List<AgentTask> completedTasks = new List<AgentTask>();
        private bool isProcessing = false;

        // optional sibling components
        private RAGManager ragManager;
        private SubAgentManager subAgentManager;

        public event Action<AgentTask> OnTaskStarted;
        public event Action<AgentTask> OnTaskCompleted;
        public event Action<AgentTask> OnTaskFailed;
        public event Action<string> OnPlanGenerated;

        public AIAgentConfig Config => config;
        public List<AgentTask> RunningTasks => runningTasks;
        public List<AgentTask> CompletedTasks => completedTasks;
        public bool IsProcessing => isProcessing;
        public string AgentName => agentName;

        private void Awake()
        {
            ragManager = GetComponent<RAGManager>();
            subAgentManager = GetComponent<SubAgentManager>();

            // Register self with sub-agent manager (if present)
            if (subAgentManager != null && !string.IsNullOrEmpty(agentName))
                subAgentManager.RegisterAgent(agentName, this);
        }

        private void Update()
        {
            if (isProcessing)
                ProcessTasks();
        }

        // ------------------------------------------------------------------ public API

        /// <summary>
        /// Execute a natural-language command: plan tasks with the LLM and queue them.
        /// Returns the list of planned tasks.
        /// </summary>
        public async Task<List<AgentTask>> ExecuteCommand(string command)
        {
            Debug.Log($"[AIAgent:{agentName}] Executing command: {command}");

            var tasks = await PlanTasks(command);

            foreach (var task in tasks)
                taskQueue.Enqueue(task);

            isProcessing = true;
            return tasks;
        }

        /// <summary>
        /// Delegate a task to a named sub-agent via the <see cref="SubAgentManager"/>.
        /// </summary>
        public async Task<string> DelegateToSubAgent(string subAgentName, AgentTask task)
        {
            if (subAgentManager == null)
                throw new Exception("[AIAgent] No SubAgentManager present on this GameObject.");

            return await subAgentManager.DelegateTask(subAgentName, task);
        }

        /// <summary>Cancel all queued and running tasks.</summary>
        public void CancelAllTasks()
        {
            taskQueue.Clear();

            foreach (var task in runningTasks)
                task.Status = TaskStatus.Cancelled;

            runningTasks.Clear();
            isProcessing = false;

            Debug.Log($"[AIAgent:{agentName}] All tasks cancelled");
        }

        // ------------------------------------------------------------------ planning

        private async Task<List<AgentTask>> PlanTasks(string command)
        {
            var tasks = new List<AgentTask>();

            try
            {
                var llmResponse = await CallLLMForPlanning(command);
                tasks = ParseTasksFromLLMResponse(llmResponse);
                OnPlanGenerated?.Invoke($"Generated {tasks.Count} task(s) from command");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIAgent:{agentName}] Planning error: {ex.Message}");

                // Fallback: single task
                tasks.Add(new AgentTask
                {
                    Name = "Execute Command",
                    Description = command,
                    ExecutionMode = TaskExecutionMode.Serial
                });
            }

            return tasks;
        }

        private async Task<string> CallLLMForPlanning(string command)
        {
            if (config?.ModelApiConfig == null)
                throw new Exception("AIAgentConfig or ModelApiConfig is not set");

            var llmClient = new LLMClient(config.ModelApiConfig);

            // Retrieve RAG context if available
            var ragContext = string.Empty;
            if (ragManager != null && (config.RAGConfig?.Enabled ?? false))
                ragContext = ragManager.BuildContext(command);

            var systemPrompt = BuildPlanningSystemPrompt(ragContext);
            return await llmClient.GenerateResponse(systemPrompt, command);
        }

        private string BuildPlanningSystemPrompt(string ragContext)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("You are a task planning AI. Given a command, break it down into specific, executable tasks.");
            sb.AppendLine("Return tasks in JSON format:");
            sb.AppendLine("[");
            sb.AppendLine("  {");
            sb.AppendLine("    \"name\": \"Task name\",");
            sb.AppendLine("    \"description\": \"Task description\",");
            sb.AppendLine("    \"executionMode\": \"Serial\" or \"Parallel\",");
            sb.AppendLine("    \"dependencies\": [\"task_id_1\"]");
            sb.AppendLine("  }");
            sb.AppendLine("]");

            if (!string.IsNullOrEmpty(ragContext))
            {
                sb.AppendLine();
                sb.Append(ragContext);
            }

            return sb.ToString();
        }

        private List<AgentTask> ParseTasksFromLLMResponse(string response)
        {
            var tasks = new List<AgentTask>();

            try
            {
                var taskDefs = JsonUtility.FromJson<TaskDefinitionList>($"{{\"tasks\":{response}}}");

                foreach (var taskDef in taskDefs.tasks)
                {
                    var task = new AgentTask
                    {
                        Name = taskDef.name,
                        Description = taskDef.description,
                        ExecutionMode = taskDef.executionMode == "Parallel" ?
                            TaskExecutionMode.Parallel : TaskExecutionMode.Serial
                    };

                    if (taskDef.dependencies != null)
                        task.Dependencies.AddRange(taskDef.dependencies);

                    tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIAgent:{agentName}] Failed to parse LLM response: {ex.Message}");
            }

            return tasks;
        }

        // ------------------------------------------------------------------ task processing

        private void ProcessTasks()
        {
            runningTasks.RemoveAll(t => t.Status == TaskStatus.Completed ||
                                        t.Status == TaskStatus.Failed ||
                                        t.Status == TaskStatus.Cancelled);

            while (taskQueue.Count > 0)
            {
                var task = taskQueue.Peek();
                if (CanStartTask(task))
                {
                    taskQueue.Dequeue();
                    StartTask(task);
                }
                else
                {
                    break;
                }
            }

            if (taskQueue.Count == 0 && runningTasks.Count == 0)
            {
                isProcessing = false;
                Debug.Log($"[AIAgent:{agentName}] All tasks completed");
            }
        }

        private bool CanStartTask(AgentTask task)
        {
            foreach (var depId in task.Dependencies)
            {
                var dep = completedTasks.Find(t => t.Id == depId);
                if (dep == null || dep.Status != TaskStatus.Completed)
                    return false;
            }

            if (task.ExecutionMode == TaskExecutionMode.Serial && runningTasks.Count > 0)
                return false;

            return true;
        }

        private async void StartTask(AgentTask task)
        {
            task.Status = TaskStatus.Running;
            runningTasks.Add(task);
            OnTaskStarted?.Invoke(task);

            Debug.Log($"[AIAgent:{agentName}] Starting task: {task.Name}");

            try
            {
                await ExecuteTask(task);

                task.Status = TaskStatus.Completed;
                task.Progress = 1.0f;
                completedTasks.Add(task);
                OnTaskCompleted?.Invoke(task);

                Debug.Log($"[AIAgent:{agentName}] Completed task: {task.Name}");
            }
            catch (Exception ex)
            {
                task.Status = TaskStatus.Failed;
                task.Error = ex.Message;
                completedTasks.Add(task);
                OnTaskFailed?.Invoke(task);

                Debug.LogError($"[AIAgent:{agentName}] Task failed: {task.Name} - {ex.Message}");
            }
        }

        private async Task ExecuteTask(AgentTask task)
        {
            var skillExecutor = GetComponent<SkillExecutor>();
            if (skillExecutor != null)
            {
                task.Result = await skillExecutor.ExecuteTask(task);
            }
            else
            {
                await Task.Delay(1000);
                task.Result = $"Executed: {task.Description}";
            }
        }
    }

    // ------------------------------------------------------------------ serialisation helpers

    [Serializable]
    class TaskDefinitionList
    {
        public List<TaskDefinition> tasks;
    }

    [Serializable]
    class TaskDefinition
    {
        public string name;
        public string description;
        public string executionMode;
        public List<string> dependencies;
    }
}

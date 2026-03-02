using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Base class for AI Agent with task planning and execution capabilities
    /// </summary>
    public class AIAgent : MonoBehaviour
    {
        [SerializeField] private AIAgentConfig config;
        private Queue<AgentTask> taskQueue = new Queue<AgentTask>();
        private List<AgentTask> runningTasks = new List<AgentTask>();
        private List<AgentTask> completedTasks = new List<AgentTask>();
        private bool isProcessing = false;

        public event Action<AgentTask> OnTaskStarted;
        public event Action<AgentTask> OnTaskCompleted;
        public event Action<AgentTask> OnTaskFailed;
        public event Action<string> OnPlanGenerated;

        public AIAgentConfig Config => config;
        public List<AgentTask> RunningTasks => runningTasks;
        public List<AgentTask> CompletedTasks => completedTasks;
        public bool IsProcessing => isProcessing;

        private void Update()
        {
            if (isProcessing)
            {
                ProcessTasks();
            }
        }

        /// <summary>
        /// Execute a command and plan tasks
        /// </summary>
        public async Task<List<AgentTask>> ExecuteCommand(string command)
        {
            Debug.Log($"[AIAgent] Executing command: {command}");
            
            // Generate task plan from command
            var tasks = await PlanTasks(command);
            
            // Add tasks to queue
            foreach (var task in tasks)
            {
                taskQueue.Enqueue(task);
            }
            
            // Start processing
            isProcessing = true;
            
            return tasks;
        }

        /// <summary>
        /// Plan tasks based on the command using AI
        /// </summary>
        private async Task<List<AgentTask>> PlanTasks(string command)
        {
            var tasks = new List<AgentTask>();
            
            try
            {
                // Call LLM API to generate task plan
                var llmResponse = await CallLLMForPlanning(command);
                
                // Parse response and create tasks
                tasks = ParseTasksFromLLMResponse(llmResponse);
                
                OnPlanGenerated?.Invoke($"Generated {tasks.Count} tasks from command");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIAgent] Error planning tasks: {ex.Message}");
                
                // Fallback: Create a single task
                tasks.Add(new AgentTask
                {
                    Name = "Execute Command",
                    Description = command,
                    ExecutionMode = TaskExecutionMode.Serial
                });
            }
            
            return tasks;
        }

        /// <summary>
        /// Call LLM API for task planning
        /// </summary>
        private async Task<string> CallLLMForPlanning(string command)
        {
            if (config == null || config.ModelApiConfig == null)
            {
                throw new Exception("AIAgent config or ModelApiConfig is not set");
            }

            var llmClient = new LLMClient(config.ModelApiConfig);
            
            var systemPrompt = @"You are a task planning AI. Given a command, break it down into specific, executable tasks.
Return tasks in JSON format:
[
  {
    ""name"": ""Task name"",
    ""description"": ""Task description"",
    ""executionMode"": ""Serial"" or ""Parallel"",
    ""dependencies"": [""task_id_1"", ""task_id_2""]
  }
]";
            
            return await llmClient.GenerateResponse(systemPrompt, command);
        }

        /// <summary>
        /// Parse tasks from LLM response
        /// </summary>
        private List<AgentTask> ParseTasksFromLLMResponse(string response)
        {
            var tasks = new List<AgentTask>();
            
            try
            {
                // Simple JSON parsing (in production, use JsonUtility or Newtonsoft.Json)
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
                    {
                        task.Dependencies.AddRange(taskDef.dependencies);
                    }
                    
                    tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIAgent] Failed to parse LLM response: {ex.Message}");
                Debug.LogWarning($"Response was: {response}");
            }
            
            return tasks;
        }

        /// <summary>
        /// Process queued tasks
        /// </summary>
        private void ProcessTasks()
        {
            // Remove completed tasks from running list
            runningTasks.RemoveAll(t => t.Status == TaskStatus.Completed || 
                                        t.Status == TaskStatus.Failed || 
                                        t.Status == TaskStatus.Cancelled);
            
            // Check if we can start new tasks
            while (taskQueue.Count > 0)
            {
                var task = taskQueue.Peek();
                
                // Check dependencies
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
            
            // Stop processing if no more tasks
            if (taskQueue.Count == 0 && runningTasks.Count == 0)
            {
                isProcessing = false;
                Debug.Log("[AIAgent] All tasks completed");
            }
        }

        /// <summary>
        /// Check if a task can be started
        /// </summary>
        private bool CanStartTask(AgentTask task)
        {
            // Check if dependencies are satisfied
            foreach (var depId in task.Dependencies)
            {
                var depTask = completedTasks.Find(t => t.Id == depId);
                if (depTask == null || depTask.Status != TaskStatus.Completed)
                {
                    return false;
                }
            }
            
            // For serial execution, check if any tasks are running
            if (task.ExecutionMode == TaskExecutionMode.Serial && runningTasks.Count > 0)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Start executing a task
        /// </summary>
        private async void StartTask(AgentTask task)
        {
            task.Status = TaskStatus.Running;
            runningTasks.Add(task);
            OnTaskStarted?.Invoke(task);
            
            Debug.Log($"[AIAgent] Starting task: {task.Name}");
            
            try
            {
                // Execute the task
                await ExecuteTask(task);
                
                task.Status = TaskStatus.Completed;
                task.Progress = 1.0f;
                completedTasks.Add(task);
                OnTaskCompleted?.Invoke(task);
                
                Debug.Log($"[AIAgent] Completed task: {task.Name}");
            }
            catch (Exception ex)
            {
                task.Status = TaskStatus.Failed;
                task.Error = ex.Message;
                completedTasks.Add(task);
                OnTaskFailed?.Invoke(task);
                
                Debug.LogError($"[AIAgent] Task failed: {task.Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a single task
        /// </summary>
        private async Task ExecuteTask(AgentTask task)
        {
            // Use skills or MCP tools to execute the task
            var skillExecutor = GetComponent<SkillExecutor>();
            if (skillExecutor != null)
            {
                var result = await skillExecutor.ExecuteTask(task);
                task.Result = result;
            }
            else
            {
                // Simulate task execution
                await Task.Delay(1000);
                task.Result = $"Executed: {task.Description}";
            }
        }

        /// <summary>
        /// Cancel all tasks
        /// </summary>
        public void CancelAllTasks()
        {
            taskQueue.Clear();
            
            foreach (var task in runningTasks)
            {
                task.Status = TaskStatus.Cancelled;
            }
            
            runningTasks.Clear();
            isProcessing = false;
            
            Debug.Log("[AIAgent] All tasks cancelled");
        }
    }

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

# API Reference

## Unity3DAgent.Core Namespace

### AIAgent Class

AI Agent的核心类，负责任务规划和执行。

#### Properties

```csharp
public AIAgentConfig Config { get; }
```
获取当前Agent的配置。

```csharp
public List<AgentTask> RunningTasks { get; }
```
获取当前正在运行的任务列表。

```csharp
public List<AgentTask> CompletedTasks { get; }
```
获取已完成的任务列表。

```csharp
public bool IsProcessing { get; }
```
指示Agent是否正在处理任务。

#### Events

```csharp
public event Action<AgentTask> OnTaskStarted;
```
任务开始时触发。

```csharp
public event Action<AgentTask> OnTaskCompleted;
```
任务完成时触发。

```csharp
public event Action<AgentTask> OnTaskFailed;
```
任务失败时触发。

```csharp
public event Action<string> OnPlanGenerated;
```
生成任务计划时触发。

#### Methods

```csharp
public async Task<List<AgentTask>> ExecuteCommand(string command)
```
执行一个命令并生成任务计划。

**参数:**
- `command`: 要执行的命令字符串

**返回值:**
- `Task<List<AgentTask>>`: 生成的任务列表

**示例:**
```csharp
var agent = GetComponent<AIAgent>();
var tasks = await agent.ExecuteCommand("创建一个红色的立方体");
```

```csharp
public void CancelAllTasks()
```
取消所有正在执行和队列中的任务。

**示例:**
```csharp
agent.CancelAllTasks();
```

---

### AgentTask Class

表示一个AI Agent任务。

#### Properties

```csharp
public string Id { get; set; }
```
任务的唯一标识符。

```csharp
public string Name { get; set; }
```
任务名称。

```csharp
public string Description { get; set; }
```
任务描述。

```csharp
public TaskStatus Status { get; set; }
```
任务状态（Pending, Running, Completed, Failed, Cancelled）。

```csharp
public TaskExecutionMode ExecutionMode { get; set; }
```
执行模式（Serial, Parallel）。

```csharp
public List<string> Dependencies { get; set; }
```
任务依赖的其他任务ID列表。

```csharp
public Dictionary<string, object> Parameters { get; set; }
```
任务参数。

```csharp
public float Progress { get; set; }
```
任务进度（0.0 - 1.0）。

```csharp
public string Result { get; set; }
```
任务执行结果。

```csharp
public string Error { get; set; }
```
任务错误信息（如果失败）。

---

### AIAgentConfig Class

AI Agent配置ScriptableObject。

#### Properties

```csharp
public ModelApiConfig ModelApiConfig { get; set; }
```
大模型API配置。

```csharp
public List<string> SkillDirectories { get; set; }
```
技能目录列表。

```csharp
public List<SkillToggle> SkillToggles { get; set; }
```
技能开关配置列表。

```csharp
public List<MCPServerConfig> MCPServers { get; set; }
```
MCP服务器配置列表。

```csharp
public int MaxParallelTasks { get; set; }
```
最大并行任务数。

```csharp
public float TaskTimeoutSeconds { get; set; }
```
任务超时时间（秒）。

---

### LLMClient Class

与大语言模型API通信的客户端。

#### Constructor

```csharp
public LLMClient(ModelApiConfig config)
```
创建LLM客户端实例。

**参数:**
- `config`: 模型API配置

#### Methods

```csharp
public async Task<string> GenerateResponse(string systemPrompt, string userPrompt)
```
生成LLM响应。

**参数:**
- `systemPrompt`: 系统提示
- `userPrompt`: 用户提示

**返回值:**
- `Task<string>`: LLM生成的响应

**示例:**
```csharp
var llmClient = new LLMClient(config.ModelApiConfig);
var response = await llmClient.GenerateResponse(
    "You are a helpful assistant.",
    "What is Unity?"
);
```

---

### Skill Class (Abstract)

技能基类。

#### Properties

```csharp
public string SkillName { get; set; }
```
技能名称。

```csharp
public string Description { get; set; }
```
技能描述。

```csharp
public List<string> Keywords { get; set; }
```
技能关键词。

```csharp
public bool Enabled { get; set; }
```
技能是否启用。

#### Abstract Methods

```csharp
public abstract bool CanHandle(AgentTask task, string context);
```
判断技能是否可以处理给定任务。

**参数:**
- `task`: 要处理的任务
- `context`: 上下文信息

**返回值:**
- `bool`: 如果可以处理返回true

```csharp
public abstract Task<string> Execute(AgentTask task, Dictionary<string, object> parameters);
```
执行技能。

**参数:**
- `task`: 要执行的任务
- `parameters`: 执行参数

**返回值:**
- `Task<string>`: 执行结果

#### Virtual Methods

```csharp
public virtual string GetCapabilityDescription()
```
获取技能能力描述（用于LLM）。

**返回值:**
- `string`: 能力描述

**示例 - 创建自定义技能:**
```csharp
[CreateAssetMenu(fileName = "MySkill", menuName = "Unity3DAgent/Skills/My Skill")]
public class MyCustomSkill : Skill
{
    public override bool CanHandle(AgentTask task, string context)
    {
        return task.Description.Contains("custom");
    }

    public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
    {
        // 执行逻辑
        await Task.Delay(1000);
        return "Skill executed successfully";
    }
}
```

---

### SkillManager Class

技能管理器。

#### Properties

```csharp
public List<Skill> LoadedSkills { get; }
```
已加载的技能列表。

#### Methods

```csharp
public void LoadSkills()
```
从配置的目录加载技能。

```csharp
public Skill FindSkillForTask(AgentTask task, string context = "")
```
查找可以处理指定任务的技能。

**参数:**
- `task`: 任务
- `context`: 上下文（可选）

**返回值:**
- `Skill`: 找到的技能，如果没有则返回null

```csharp
public string GetAllSkillDescriptions()
```
获取所有已启用技能的描述。

**返回值:**
- `string`: 所有技能描述的组合字符串

```csharp
public void ReloadSkills()
```
重新加载技能。

---

### SkillExecutor Class

技能执行器。

#### Methods

```csharp
public async Task<string> ExecuteTask(AgentTask task)
```
使用合适的技能执行任务。

**参数:**
- `task`: 要执行的任务

**返回值:**
- `Task<string>`: 执行结果

---

### MCPClient Class

Model Context Protocol客户端。

#### Properties

```csharp
public bool IsConnected { get; }
```
是否连接到MCP服务器。

```csharp
public List<MCPTool> AvailableTools { get; }
```
可用的MCP工具列表。

```csharp
public List<MCPResource> AvailableResources { get; }
```
可用的MCP资源列表。

```csharp
public List<MCPPrompt> AvailablePrompts { get; }
```
可用的MCP提示词列表。

#### Methods

```csharp
public async void ConnectToServers()
```
连接到所有配置的MCP服务器。

```csharp
public async Task<string> ExecuteTask(AgentTask task)
```
使用MCP工具执行任务。

**参数:**
- `task`: 要执行的任务

**返回值:**
- `Task<string>`: 执行结果

```csharp
public async Task<string> GetResource(string resourceName)
```
从MCP服务器获取资源。

**参数:**
- `resourceName`: 资源名称

**返回值:**
- `Task<string>`: 资源内容

```csharp
public async Task<string> GetPrompt(string promptName, Dictionary<string, string> parameters = null)
```
从MCP服务器获取提示词。

**参数:**
- `promptName`: 提示词名称
- `parameters`: 提示词参数（可选）

**返回值:**
- `Task<string>`: 提示词内容

```csharp
public void DisconnectAll()
```
断开所有MCP服务器连接。

**示例:**
```csharp
var mcpClient = GetComponent<MCPClient>();

// 获取资源
var config = await mcpClient.GetResource("app-config.json");

// 获取提示词
var prompt = await mcpClient.GetPrompt("code-generation", new Dictionary<string, string>
{
    { "language", "csharp" },
    { "task", "create button" }
});
```

---

## Unity3DAgent.Editor Namespace

### AIAgentWindow Class

Unity编辑器窗口。

#### Static Methods

```csharp
[MenuItem("Window/AI Agent")]
public static void ShowWindow()
```
显示AI Agent编辑器窗口。

**使用方式:**
在Unity编辑器中，选择 `Window > AI Agent` 打开窗口。

---

## Enums

### TaskStatus

```csharp
public enum TaskStatus
{
    Pending,      // 待处理
    Running,      // 运行中
    Completed,    // 已完成
    Failed,       // 失败
    Cancelled     // 已取消
}
```

### TaskExecutionMode

```csharp
public enum TaskExecutionMode
{
    Serial,       // 串行执行
    Parallel      // 并行执行
}
```

### ModelProvider

```csharp
public enum ModelProvider
{
    Ollama,       // Ollama本地模型
    OpenAI,       // OpenAI API
    Anthropic,    // Anthropic Claude
    Custom        // 自定义API
}
```

---

## 使用模式

### 基本使用

```csharp
// 1. 获取或创建Agent
var agent = FindObjectOfType<AIAgent>();
if (agent == null)
{
    var agentObj = new GameObject("AI Agent");
    agent = agentObj.AddComponent<AIAgent>();
    agentObj.AddComponent<SkillManager>();
    agentObj.AddComponent<SkillExecutor>();
    agentObj.AddComponent<MCPClient>();
}

// 2. 订阅事件
agent.OnTaskCompleted += (task) => 
{
    Debug.Log($"Task completed: {task.Name}");
};

// 3. 执行命令
await agent.ExecuteCommand("创建一个游戏对象");
```

### 创建自定义技能

```csharp
using Unity3DAgent.Core;
using System.Threading.Tasks;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Unity3DAgent/Skills/Custom")]
public class CustomSkill : Skill
{
    [Header("Custom Settings")]
    public string customParameter;

    public override bool CanHandle(AgentTask task, string context)
    {
        // 实现匹配逻辑
        return task.Description.ToLower().Contains("custom");
    }

    public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
    {
        // 实现执行逻辑
        Debug.Log($"Executing {SkillName}");
        await Task.Delay(1000);
        return "Success";
    }

    public override string GetCapabilityDescription()
    {
        return $"{SkillName}: {Description} - {customParameter}";
    }
}
```

### 配置MCP服务器

```csharp
// 在 AIAgentConfig ScriptableObject 中
var mcpServer = new MCPServerConfig
{
    Name = "My MCP Server",
    Url = "http://localhost:3000",
    Enabled = true,
    Parameters = new Dictionary<string, string>
    {
        { "apiKey", "your-api-key" },
        { "version", "1.0" }
    }
};

config.MCPServers.Add(mcpServer);
```

---

## 错误处理

所有异步方法都可能抛出异常。建议使用try-catch包装：

```csharp
try
{
    await agent.ExecuteCommand("command");
}
catch (Exception ex)
{
    Debug.LogError($"Error executing command: {ex.Message}");
}
```

---

## 性能建议

1. 限制并行任务数量（MaxParallelTasks）
2. 设置合理的超时时间（TaskTimeoutSeconds）
3. 定期清理已完成的任务
4. 使用事件而不是轮询检查状态
5. 缓存技能实例避免重复加载

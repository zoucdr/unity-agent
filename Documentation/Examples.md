# Unity3D AI Agent - 使用示例

## 基础示例

### 示例 1: 创建游戏对象

```csharp
// 在命令输入框中输入：
创建一个名为Player的游戏对象，位置在(0, 1, 0)

// AI Agent将自动：
// 1. 解析命令
// 2. 创建任务计划
// 3. 使用SceneOperationSkill执行
```

### 示例 2: 批量操作

```csharp
// 命令：
查找场景中所有的灯光，并将它们的强度设置为2

// AI Agent将：
// 1. 查找所有Light组件
// 2. 遍历并修改intensity属性
// 3. 报告修改的数量
```

## 自定义技能示例

### 文本分析技能

```csharp
using Unity3DAgent.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TextAnalysisSkill", menuName = "Unity3DAgent/Skills/Text Analysis")]
public class TextAnalysisSkill : Skill
{
    public override bool CanHandle(AgentTask task, string context)
    {
        if (!Enabled) return false;
        
        var desc = task.Description.ToLower();
        return desc.Contains("分析") || desc.Contains("analyze") || 
               desc.Contains("文本") || desc.Contains("text");
    }

    public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
    {
        Debug.Log($"[TextAnalysisSkill] 分析文本...");
        
        // 模拟文本分析
        await Task.Delay(1000);
        
        var result = new
        {
            wordCount = 150,
            sentiment = "positive",
            keywords = new[] { "unity", "agent", "ai" }
        };
        
        return JsonUtility.ToJson(result);
    }
}
```

### 性能优化技能

```csharp
using Unity3DAgent.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PerformanceOptimizationSkill", menuName = "Unity3DAgent/Skills/Performance")]
public class PerformanceOptimizationSkill : Skill
{
    public override bool CanHandle(AgentTask task, string context)
    {
        if (!Enabled) return false;
        
        var desc = task.Description.ToLower();
        return desc.Contains("优化") || desc.Contains("optimize") || 
               desc.Contains("性能") || desc.Contains("performance");
    }

    public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
    {
        Debug.Log($"[PerformanceOptimizationSkill] 开始性能优化...");
        
        var optimizations = new List<string>();
        
        // 优化渲染设置
        QualitySettings.shadowDistance = 50;
        optimizations.Add("调整阴影距离");
        
        // 优化物理设置
        Physics.autoSimulation = true;
        optimizations.Add("启用物理自动模拟");
        
        await Task.Delay(500);
        
        return $"完成优化: {string.Join(", ", optimizations)}";
    }
}
```

## 并行任务示例

```csharp
// 在代码中创建并行任务
var tasks = new List<AgentTask>
{
    new AgentTask
    {
        Name = "加载资源A",
        Description = "从Resources加载prefabA",
        ExecutionMode = TaskExecutionMode.Parallel
    },
    new AgentTask
    {
        Name = "加载资源B",
        Description = "从Resources加载prefabB",
        ExecutionMode = TaskExecutionMode.Parallel
    },
    new AgentTask
    {
        Name = "初始化场景",
        Description = "设置场景初始状态",
        ExecutionMode = TaskExecutionMode.Serial,
        Dependencies = new List<string> { "task_id_a", "task_id_b" }
    }
};
```

## MCP集成示例

### 连接到MCP服务器

```csharp
// 在AI Agent Config中配置MCP服务器
var mcpConfig = new MCPServerConfig
{
    Name = "FileSystem MCP",
    Url = "http://localhost:3000",
    Enabled = true,
    Parameters = new Dictionary<string, string>
    {
        { "rootPath", "/project/data" }
    }
};

config.MCPServers.Add(mcpConfig);
```

### 使用MCP工具

```csharp
// 获取MCP客户端
var mcpClient = agent.GetComponent<MCPClient>();

// 获取资源
var fileContent = await mcpClient.GetResource("config.json");

// 获取提示词
var prompt = await mcpClient.GetPrompt("generate_code", new Dictionary<string, string>
{
    { "language", "csharp" },
    { "task", "create ui button" }
});
```

## 事件处理示例

```csharp
using Unity3DAgent.Core;
using UnityEngine;

public class AgentEventHandler : MonoBehaviour
{
    private AIAgent agent;

    private void Start()
    {
        agent = GetComponent<AIAgent>();
        
        // 订阅事件
        agent.OnTaskStarted += OnTaskStarted;
        agent.OnTaskCompleted += OnTaskCompleted;
        agent.OnTaskFailed += OnTaskFailed;
        agent.OnPlanGenerated += OnPlanGenerated;
    }

    private void OnTaskStarted(AgentTask task)
    {
        Debug.Log($"任务开始: {task.Name}");
        // 显示进度条或通知
    }

    private void OnTaskCompleted(AgentTask task)
    {
        Debug.Log($"任务完成: {task.Name} - {task.Result}");
        // 更新UI或触发后续操作
    }

    private void OnTaskFailed(AgentTask task)
    {
        Debug.LogError($"任务失败: {task.Name} - {task.Error}");
        // 显示错误消息
    }

    private void OnPlanGenerated(string message)
    {
        Debug.Log($"任务规划: {message}");
        // 显示任务计划
    }

    private void OnDestroy()
    {
        if (agent != null)
        {
            agent.OnTaskStarted -= OnTaskStarted;
            agent.OnTaskCompleted -= OnTaskCompleted;
            agent.OnTaskFailed -= OnTaskFailed;
            agent.OnPlanGenerated -= OnPlanGenerated;
        }
    }
}
```

## 配置文件示例

### 完整的Agent配置

```csharp
// 创建 AIAgentConfig 资源并设置如下：

// 模型API配置
ModelApiConfig:
  Provider: Ollama
  BaseUrl: http://localhost:11434
  ModelName: llama2
  ApiKey: (留空)
  Temperature: 0.7
  MaxTokens: 2048

// 技能目录
SkillDirectories:
  - Assets/Skills/Core
  - Assets/Skills/Custom
  - Assets/Skills/Editor

// 技能开关
SkillToggles:
  - SkillName: FileOperationSkill
    Enabled: true
  - SkillName: SceneOperationSkill
    Enabled: true
  - SkillName: TextAnalysisSkill
    Enabled: false

// MCP服务器
MCPServers:
  - Name: FileSystem
    Url: http://localhost:3000
    Enabled: true
  - Name: Database
    Url: http://localhost:3001
    Enabled: false

// 执行设置
MaxParallelTasks: 5
TaskTimeoutSeconds: 300
```

## 调试技巧

### 启用详细日志

```csharp
// 在 AIAgent 中添加日志级别
Debug.Log("[AIAgent] Starting task execution...");
Debug.LogWarning("[AIAgent] Task execution delayed...");
Debug.LogError("[AIAgent] Task execution failed!");
```

### 监控任务状态

```csharp
// 在编辑器中实时查看
private void OnGUI()
{
    if (agent == null) return;
    
    GUI.Label(new Rect(10, 10, 200, 20), $"Running: {agent.RunningTasks.Count}");
    GUI.Label(new Rect(10, 30, 200, 20), $"Completed: {agent.CompletedTasks.Count}");
    GUI.Label(new Rect(10, 50, 200, 20), $"Status: {agent.IsProcessing}");
}
```

## 性能优化建议

1. **限制并行任务数量**: 在配置中设置合适的 `MaxParallelTasks`
2. **使用任务超时**: 设置 `TaskTimeoutSeconds` 避免任务卡死
3. **技能缓存**: SkillManager 会缓存已加载的技能
4. **异步执行**: 所有长时间操作都使用 async/await
5. **资源管理**: 及时释放不需要的资源

## 常见问题

### Q: 如何创建复杂的任务依赖？

```csharp
var taskA = new AgentTask { Id = "task_a", Name = "Task A" };
var taskB = new AgentTask { Id = "task_b", Name = "Task B" };
var taskC = new AgentTask 
{ 
    Id = "task_c", 
    Name = "Task C",
    Dependencies = new List<string> { "task_a", "task_b" }
};
```

### Q: 如何处理任务失败？

订阅 `OnTaskFailed` 事件并实现重试逻辑：

```csharp
agent.OnTaskFailed += async (task) =>
{
    if (task.RetryCount < 3)
    {
        task.RetryCount++;
        await agent.ExecuteCommand(task.Description);
    }
};
```

### Q: 如何扩展LLM提供商？

在 `LLMClient.cs` 中添加新的提供商类型和实现方法。

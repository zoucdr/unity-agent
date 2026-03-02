# Unity3D AI Agent - 架构文档

## 系统架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                     Unity Editor Window                      │
│  ┌────────────┬────────────┬────────────┬────────────┐     │
│  │   Tasks    │   Config   │   Skills   │    MCP     │     │
│  └────────────┴────────────┴────────────┴────────────┘     │
│                      Command Input                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                         AI Agent                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │              Task Planning & Execution                 │ │
│  │  • Command parsing                                     │ │
│  │  • LLM-based task planning                            │ │
│  │  • Task queue management                              │ │
│  │  • Dependency resolution                              │ │
│  │  • Parallel/Serial execution                          │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
           │                    │                    │
           ▼                    ▼                    ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│   LLM Client     │  │  Skill Manager   │  │   MCP Client     │
│                  │  │                  │  │                  │
│ • Ollama         │  │ • Skill loading  │  │ • Server conn    │
│ • OpenAI         │  │ • Skill matching │  │ • Tool exec      │
│ • Anthropic      │  │ • Skill exec     │  │ • Resource get   │
│ • Custom API     │  │ • Context aware  │  │ • Prompt get     │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

## 核心组件

### 1. AIAgent

**职责**: 
- 任务规划与执行的核心控制器
- 管理任务队列和任务生命周期
- 协调Skills和MCP工具的使用

**主要方法**:
```csharp
public async Task<List<AgentTask>> ExecuteCommand(string command)
public void CancelAllTasks()
private async Task<List<AgentTask>> PlanTasks(string command)
private void ProcessTasks()
```

**状态管理**:
```
Idle → Processing → Idle
  ↓         ↓
Task Queue → Running Tasks → Completed Tasks
```

### 2. Task System

#### AgentTask
任务的数据结构：
```csharp
{
    Id: string,
    Name: string,
    Description: string,
    Status: TaskStatus (Pending/Running/Completed/Failed/Cancelled),
    ExecutionMode: TaskExecutionMode (Serial/Parallel),
    Dependencies: List<string>,
    Parameters: Dictionary<string, object>,
    Progress: float,
    Result: string,
    Error: string
}
```

#### Task Execution Flow
```
1. Command Input
   ↓
2. LLM Task Planning
   ↓
3. Task Queue
   ↓
4. Dependency Check
   ↓
5. Skill/MCP Selection
   ↓
6. Task Execution
   ↓
7. Result Collection
   ↓
8. Status Update
```

### 3. LLM Client

**支持的提供商**:
- **Ollama**: 本地大模型（主要支持）
- **OpenAI**: GPT系列模型
- **Anthropic**: Claude系列模型
- **Custom**: 自定义API

**API调用流程**:
```
Request → Provider Router → API Adapter → HTTP Client → LLM API
                                                           ↓
Response ← JSON Parser ← Response Handler ← HTTP Response ←
```

**配置参数**:
```csharp
{
    Provider: ModelProvider,
    BaseUrl: string,
    ModelName: string,
    ApiKey: string,
    Temperature: float (0.0-1.0),
    MaxTokens: int
}
```

### 4. Skills System

#### Skill Architecture
```
Skill (Abstract Base Class)
    ↓
    ├── FileOperationSkill
    ├── SceneOperationSkill
    ├── CustomSkill1
    └── CustomSkill2
```

#### Skill Loading Process
```
1. Scan Resources/Skills directories
   ↓
2. Load all Skill ScriptableObjects
   ↓
3. Check SkillToggle in Config
   ↓
4. Cache enabled skills
   ↓
5. Register skill capabilities
```

#### Skill Selection
```
Task → SkillManager.FindSkillForTask()
         ↓
       For each skill:
         skill.CanHandle(task, context)?
           ↓
         Yes → Return skill
           ↓
         No → Continue
```

### 5. MCP Client

#### MCP Protocol Support
```
MCP Server
    ↓
    ├── Tools (executable functions)
    ├── Resources (data sources)
    └── Prompts (templated prompts)
```

#### Connection Management
```
1. Read MCP Server Config
   ↓
2. Initialize connection (HTTP/WebSocket)
   ↓
3. Fetch capabilities (tools/resources/prompts)
   ↓
4. Register available features
   ↓
5. Ready for use
```

#### Tool Execution Flow
```
Task → MCPClient.ExecuteTask()
         ↓
       FindToolForTask()
         ↓
       CallMCPTool(tool, task)
         ↓
       HTTP POST to server
         ↓
       Parse response
         ↓
       Return result
```

### 6. Unity Editor Window

#### UI Components
```
┌─────────────────────────────────────┐
│ Header                               │
│  ├── Title                           │
│  └── Status (Idle/Processing)        │
├─────────────────────────────────────┤
│ Toolbar                              │
│  ├── Tasks Tab                       │
│  ├── Config Tab                      │
│  ├── Skills Tab                      │
│  └── MCP Tab                         │
├─────────────────────────────────────┤
│ Content Area (Scrollable)            │
│  ├── Task List                       │
│  ├── Task Details                    │
│  └── Progress Indicators             │
├─────────────────────────────────────┤
│ Command Input                        │
│  ├── Text Field                      │
│  └── Execute Button                  │
└─────────────────────────────────────┘
```

## 数据流

### 命令执行数据流
```
User Input
    ↓
Editor Window
    ↓
AIAgent.ExecuteCommand(command)
    ↓
LLMClient.GenerateResponse(systemPrompt, userPrompt)
    ↓
Parse Tasks from LLM Response
    ↓
Add Tasks to Queue
    ↓
Task Processing Loop
    ↓
    ├── Check Dependencies
    ├── Check Execution Mode
    └── Start Task
         ↓
         ├── Find Skill → SkillManager
         └── Or use MCP Tool → MCPClient
              ↓
              Execute
              ↓
              Update Task Status
              ↓
              Fire Events
              ↓
              Update UI
```

### 技能执行数据流
```
Task → SkillManager.FindSkillForTask()
         ↓
       skill.CanHandle(task, context)
         ↓
       SkillExecutor.ExecuteTask()
         ↓
       skill.Execute(task, parameters)
         ↓
       Return result
         ↓
       Update task.Result
```

## 扩展点

### 1. 添加新的LLM提供商
```csharp
// 在 ModelProvider enum 中添加
public enum ModelProvider
{
    Ollama,
    OpenAI,
    Anthropic,
    YourNewProvider  // 添加新提供商
}

// 在 LLMClient 中实现
private async Task<string> CallYourNewProvider(string systemPrompt, string userPrompt)
{
    // 实现调用逻辑
}
```

### 2. 创建自定义技能
```csharp
[CreateAssetMenu(menuName = "Unity3DAgent/Skills/Your Skill")]
public class YourCustomSkill : Skill
{
    public override bool CanHandle(AgentTask task, string context)
    {
        // 实现匹配逻辑
    }

    public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
    {
        // 实现执行逻辑
    }
}
```

### 3. 扩展MCP支持
```csharp
// 添加新的MCP功能类型
public class MCPCustomFeature
{
    public string Name;
    public string ServerName;
    public Dictionary<string, object> Metadata;
    
    public async Task<string> Execute(Dictionary<string, object> parameters)
    {
        // 实现自定义功能
    }
}
```

### 4. 自定义UI面板
```csharp
// 在 AIAgentWindow 中添加新标签页
private bool showCustomPanel = false;

private void DrawCustomPanel()
{
    // 实现自定义UI
}
```

## 性能考虑

### 1. 任务执行优化
- 使用 `async/await` 实现非阻塞执行
- 限制并行任务数量 (`MaxParallelTasks`)
- 实现任务超时机制 (`TaskTimeoutSeconds`)

### 2. 技能加载优化
- 使用字典缓存已加载的技能
- 延迟加载未使用的技能
- 资源预加载策略

### 3. MCP连接优化
- 连接池管理
- 请求缓存
- 超时和重试机制

### 4. UI更新优化
- 使用事件驱动更新
- 批量更新UI元素
- 限制刷新频率

## 安全考虑

### 1. API密钥管理
- 不在代码中硬编码API密钥
- 使用Unity的安全存储
- 支持环境变量

### 2. 任务执行安全
- 验证任务参数
- 限制执行权限
- 沙箱隔离

### 3. MCP连接安全
- 验证服务器证书
- 使用安全连接(HTTPS/WSS)
- 实施访问控制

## 最佳实践

### 1. 任务设计
- 保持任务原子性
- 合理设置依赖关系
- 提供清晰的任务描述

### 2. 技能开发
- 单一职责原则
- 详细的错误处理
- 完整的日志记录

### 3. 配置管理
- 使用ScriptableObject
- 版本控制配置文件
- 提供默认配置

### 4. 调试和测试
- 启用详细日志
- 使用事件监听
- 单元测试覆盖

## 未来扩展

### 计划中的功能
1. 任务持久化和恢复
2. 分布式任务执行
3. 更多LLM提供商支持
4. 可视化任务编辑器
5. 技能市场
6. 性能监控和分析
7. 多语言支持
8. 云端配置同步

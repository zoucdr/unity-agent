# Unity3D AI Agent Plugin

AI Agent插件，用于Unity3D的智能任务规划与执行系统。

## 功能特性

### 核心功能
- **AI Agent**: 智能代理系统，支持自动任务规划与执行
- **任务管理**: 支持多任务并行和串行执行
- **大模型集成**: 支持接入多种大模型API
  - Ollama 本地大模型（主要支持）
  - OpenAI API
  - Anthropic Claude
  - 自定义API接口

### 技能系统
- 支持选择多个技能根目录
- 技能开关控制
- 通过上下文动态判断是否使用技能
- 可扩展的技能系统

### MCP客户端
- 支持连接任何MCP（Model Context Protocol）服务器
- 获取MCP工具、资源和提示词
- 动态集成外部能力

### Unity编辑器界面
- 命令输入窗口
- 动态任务图解可视化
- 实时进度追踪
- 配置管理界面

## 安装

### 通过Unity Package Manager安装

1. 打开Unity编辑器
2. 打开 Window > Package Manager
3. 点击 "+" 按钮，选择 "Add package from git URL"
4. 输入: `https://github.com/zoucdr/unity3d-agent.git`

### 手动安装

1. 下载或克隆此仓库
2. 将整个文件夹复制到你的Unity项目的 `Packages` 目录中

## 快速开始

### 1. 创建AI Agent

在Unity编辑器中：
1. 打开 `Window > AI Agent` 窗口
2. 点击 "Create Agent" 按钮
3. 这将在场景中创建一个包含所有必要组件的AI Agent游戏对象

### 2. 配置AI Agent

创建配置资源：
1. 在Project窗口中右键点击
2. 选择 `Create > Unity3DAgent > AI Agent Config`
3. 配置模型API设置：
   - Provider: 选择 Ollama、OpenAI 或其他
   - Base URL: API的基础URL（例如：`http://localhost:11434` for Ollama）
   - Model Name: 模型名称（例如：`llama2`, `gpt-4`）
   - API Key: API密钥（如果需要）

4. 将配置资源拖拽到场景中AI Agent的Config字段

### 3. 配置Ollama（本地大模型）

如果使用Ollama本地大模型：

```bash
# 安装 Ollama
curl -fsSL https://ollama.com/install.sh | sh

# 拉取模型
ollama pull llama2

# 启动 Ollama 服务
ollama serve
```

在AI Agent Config中设置：
- Provider: `Ollama`
- Base URL: `http://localhost:11434`
- Model Name: `llama2` (或你拉取的其他模型)

### 4. 创建技能

创建自定义技能：

```csharp
using Unity3DAgent.Core;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomSkill", menuName = "Unity3DAgent/Skills/Custom Skill")]
public class CustomSkill : Skill
{
    public override bool CanHandle(AgentTask task, string context)
    {
        // 判断此技能是否可以处理该任务
        return task.Description.Contains("custom");
    }

    public override async Task<string> Execute(AgentTask task, Dictionary<string, object> parameters)
    {
        // 执行技能逻辑
        Debug.Log($"Executing custom skill: {task.Description}");
        await Task.Delay(1000);
        return "Custom skill executed successfully";
    }
}
```

将技能资源保存到 `Resources/Skills` 文件夹中。

### 5. 配置MCP服务器

在AI Agent Config中添加MCP服务器：
1. 展开 MCP Servers 列表
2. 添加新的服务器配置：
   - Name: 服务器名称
   - URL: MCP服务器地址
   - Enabled: 勾选以启用

### 6. 使用AI Agent

在AI Agent窗口中：
1. 在命令输入框中输入指令
2. 点击 "Execute" 按钮
3. AI Agent会自动规划任务并执行
4. 在Tasks标签页中查看任务进度

## 示例命令

```
创建一个红色的立方体
读取配置文件并显示内容
搜索场景中所有的光源
优化场景中的渲染设置
```

## API参考

### AIAgent

主要的AI Agent组件：

```csharp
public class AIAgent : MonoBehaviour
{
    // 执行命令
    public async Task<List<AgentTask>> ExecuteCommand(string command);
    
    // 取消所有任务
    public void CancelAllTasks();
    
    // 事件
    public event Action<AgentTask> OnTaskStarted;
    public event Action<AgentTask> OnTaskCompleted;
    public event Action<AgentTask> OnTaskFailed;
}
```

### Skill

技能基类：

```csharp
public abstract class Skill : ScriptableObject
{
    // 判断是否可以处理任务
    public abstract bool CanHandle(AgentTask task, string context);
    
    // 执行技能
    public abstract Task<string> Execute(AgentTask task, Dictionary<string, object> parameters);
}
```

### MCPClient

MCP客户端：

```csharp
public class MCPClient : MonoBehaviour
{
    // 连接到服务器
    public async void ConnectToServers();
    
    // 获取资源
    public async Task<string> GetResource(string resourceName);
    
    // 获取提示词
    public async Task<string> GetPrompt(string promptName, Dictionary<string, string> parameters = null);
}
```

## 架构设计

```
Unity3DAgent
├── Runtime
│   ├── Core
│   │   ├── AIAgent.cs          - 核心AI Agent
│   │   ├── AIAgentConfig.cs    - 配置管理
│   │   ├── AgentTask.cs        - 任务定义
│   │   └── LLMClient.cs        - 大模型客户端
│   ├── Skills
│   │   ├── Skill.cs            - 技能基类
│   │   └── SkillManager.cs     - 技能管理器
│   └── MCP
│       └── MCPClient.cs        - MCP客户端
├── Editor
│   └── AIAgentWindow.cs        - Unity编辑器窗口
└── Resources
    ├── Skills                  - 技能资源
    └── Config                  - 配置资源
```

## 任务执行流程

1. 用户在UI中输入命令
2. AI Agent调用LLM生成任务计划
3. 任务被添加到任务队列
4. 任务管理器按照依赖关系执行任务
   - 串行任务：按顺序执行
   - 并行任务：同时执行
5. 对于每个任务：
   - 查找合适的技能
   - 或使用MCP工具
   - 执行并返回结果
6. 更新任务状态和进度
7. 在UI中显示执行结果

## 高级功能

### 自定义大模型提供商

```csharp
// 在 LLMClient.cs 中添加自定义提供商支持
public enum ModelProvider
{
    Ollama,
    OpenAI,
    Anthropic,
    Custom  // 添加自定义类型
}

// 实现自定义API调用
private async Task<string> CallCustomAPI(string systemPrompt, string userPrompt)
{
    // 实现你的自定义API调用逻辑
}
```

### 技能开关控制

在AI Agent Config中：
1. 展开 Skill Toggles 列表
2. 添加技能开关配置
3. 设置 Enabled 状态控制技能启用/禁用

### 上下文感知

技能的 `CanHandle` 方法接收上下文参数，可以根据当前环境动态决定是否处理任务。

## 故障排除

### 连接不到Ollama

- 确保Ollama服务正在运行：`ollama serve`
- 检查Base URL是否正确：`http://localhost:11434`
- 检查防火墙设置

### 技能不工作

- 确保技能资源在 `Resources/Skills` 文件夹中
- 检查技能是否在配置中被禁用
- 查看Console中的日志信息

### MCP连接失败

- 确认MCP服务器地址正确
- 检查网络连接
- 查看MCP服务器日志

## 许可证

MIT License

## 贡献

欢迎提交问题和拉取请求！

## 联系方式

- GitHub: https://github.com/zoucdr/unity3d-agent
- Issues: https://github.com/zoucdr/unity3d-agent/issues

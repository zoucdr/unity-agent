# Contributing to Unity3D AI Agent

感谢您对 Unity3D AI Agent 项目的关注！我们欢迎各种形式的贡献。

## 如何贡献

### 报告Bug

如果您发现了bug，请创建一个issue并包含以下信息：

1. Bug的详细描述
2. 重现步骤
3. 预期行为
4. 实际行为
5. Unity版本
6. 插件版本
7. 相关日志或截图

### 提出新功能

如果您有新功能的想法：

1. 创建一个issue描述功能需求
2. 说明使用场景和预期效果
3. 等待maintainer的反馈
4. 获得批准后开始开发

### 提交Pull Request

#### 准备工作

1. Fork此仓库
2. 创建您的功能分支 (`git checkout -b feature/AmazingFeature`)
3. 确保您的代码符合项目规范
4. 添加必要的测试
5. 更新文档

#### 代码规范

##### C#代码风格

```csharp
// 使用 PascalCase 命名类和方法
public class MyClass
{
    // 使用 camelCase 命名私有字段，使用下划线前缀
    private int _myField;
    
    // 使用 PascalCase 命名公共属性
    public int MyProperty { get; set; }
    
    // 方法命名清晰且具有描述性
    public async Task<string> ExecuteTaskAsync()
    {
        // 使用适当的异步模式
        await Task.Delay(100);
        return "result";
    }
}
```

##### 注释规范

```csharp
/// <summary>
/// 执行AI Agent任务
/// </summary>
/// <param name="command">要执行的命令</param>
/// <returns>任务列表</returns>
public async Task<List<AgentTask>> ExecuteCommand(string command)
{
    // 单行注释用于解释复杂逻辑
    var tasks = await PlanTasks(command);
    return tasks;
}
```

##### Unity特定规范

- 使用 `[SerializeField]` 而不是 public 字段
- 适当使用 `[Header]` 和 `[Tooltip]` 标记
- 清理不使用的引用（避免内存泄漏）
- 使用 Unity 的生命周期方法

```csharp
[SerializeField] 
[Tooltip("AI Agent配置")]
private AIAgentConfig config;

[Header("执行设置")]
[SerializeField] 
private int maxParallelTasks = 5;
```

#### 提交信息规范

使用语义化的提交信息：

```
<type>(<scope>): <subject>

<body>

<footer>
```

类型 (type):
- `feat`: 新功能
- `fix`: Bug修复
- `docs`: 文档更新
- `style`: 代码格式（不影响代码运行）
- `refactor`: 重构
- `perf`: 性能优化
- `test`: 测试相关
- `chore`: 构建过程或辅助工具变动

示例:
```
feat(skills): add text analysis skill

Add a new skill for analyzing text content with sentiment analysis
and keyword extraction.

Closes #123
```

#### 测试

在提交PR之前，请确保：

1. 所有现有测试通过
2. 为新功能添加测试
3. 测试覆盖率保持或提高
4. 手动测试新功能

#### 文档

如果您的更改影响了API或使用方式：

1. 更新README.md
2. 更新相关的文档文件
3. 在CHANGELOG.md中记录更改
4. 添加示例代码（如果适用）

### Pull Request流程

1. 确保您的代码遵循上述规范
2. 更新CHANGELOG.md
3. 提交PR并填写PR模板
4. 等待代码审查
5. 根据反馈进行修改
6. PR被合并

## 开发环境设置

### 要求

- Unity 2020.3 或更高版本
- .NET Standard 2.1
- Git

### 设置步骤

1. Clone仓库
```bash
git clone https://github.com/zoucdr/unity3d-agent.git
```

2. 在Unity中打开项目或添加为Package

3. 安装依赖（如果有）

4. 开始开发

## 项目结构

```
unity3d-agent/
├── Runtime/              # 运行时代码
│   ├── Core/            # 核心功能
│   ├── Skills/          # 技能系统
│   └── MCP/             # MCP客户端
├── Editor/              # 编辑器代码
├── Resources/           # 资源文件
├── Documentation/       # 文档
└── Tests/              # 测试（未来添加）
```

## 设计原则

1. **简单性**: 保持代码简单易懂
2. **可扩展性**: 设计应该易于扩展
3. **模块化**: 功能应该模块化
4. **性能**: 考虑性能影响
5. **兼容性**: 保持与Unity不同版本的兼容

## 社区准则

### 行为准则

- 尊重所有贡献者
- 欢迎不同观点
- 建设性的批评
- 专注于对项目最好的事情
- 友好和包容

### 沟通渠道

- GitHub Issues: 问题讨论和功能请求
- Pull Requests: 代码审查和讨论
- Discussions: 一般讨论和问答

## 认可贡献者

所有贡献者都会在项目的贡献者列表中得到认可。

## 问题？

如果您有任何问题，请：

1. 查看现有的issues
2. 阅读文档
3. 创建新的issue询问

感谢您的贡献！🎉

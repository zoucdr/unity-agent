# Quick Start Guide

本指南将帮助您在5分钟内开始使用Unity3D AI Agent。

## 步骤 1: 安装插件

### 方法 A: 通过 Unity Package Manager（推荐）

1. 打开Unity编辑器
2. 打开 `Window > Package Manager`
3. 点击左上角的 `+` 按钮
4. 选择 `Add package from git URL`
5. 输入: `https://github.com/zoucdr/unity3d-agent.git`
6. 点击 `Add`

### 方法 B: 手动安装

1. 下载或克隆此仓库
2. 将文件夹复制到你的Unity项目的 `Packages` 目录中
3. Unity会自动识别并导入插件

## 步骤 2: 设置 Ollama（本地大模型）

1. 安装 Ollama:
```bash
# macOS/Linux
curl -fsSL https://ollama.com/install.sh | sh

# Windows
# 从 https://ollama.com/download 下载安装程序
```

2. 拉取模型:
```bash
ollama pull llama2
# 或者使用其他模型，如:
# ollama pull mistral
# ollama pull codellama
```

3. 启动 Ollama 服务:
```bash
ollama serve
```

4. 验证安装:
```bash
curl http://localhost:11434/api/generate -d '{
  "model": "llama2",
  "prompt": "Hello!",
  "stream": false
}'
```

## 步骤 3: 在Unity中创建AI Agent

1. 打开Unity场景
2. 打开 `Window > AI Agent`
3. 在AI Agent窗口中点击 `Create Agent` 按钮
4. 这会在场景中创建一个名为 "AI Agent" 的GameObject

## 步骤 4: 创建配置

1. 在Project窗口中右键点击
2. 选择 `Create > Unity3DAgent > AI Agent Config`
3. 命名为 "DefaultAgentConfig"
4. 选中配置文件，在Inspector中设置:

```
Model API Config:
  Provider: Ollama
  Base Url: http://localhost:11434
  Model Name: llama2
  Api Key: (留空)
  Temperature: 0.7
  Max Tokens: 2048

Max Parallel Tasks: 5
Task Timeout Seconds: 300
```

5. 将配置拖拽到场景中AI Agent GameObject的Config字段

## 步骤 5: 测试AI Agent

1. 在AI Agent窗口中，确保你在 "Tasks" 标签页
2. 在底部的命令输入框中输入:
```
创建一个红色的立方体
```

3. 点击 `Execute` 按钮
4. 观察任务在UI中被创建和执行
5. 查看Unity Console获取详细日志

## 步骤 6: 创建第一个技能（可选）

1. 在Project窗口中右键点击
2. 选择 `Create > Unity3DAgent > Skills > Scene Operation`
3. 命名为 "MyFirstSkill"
4. 将技能文件移动到 `Resources/Skills` 文件夹中
5. 在AI Agent窗口的 "Skills" 标签页中点击 "Reload Skills"
6. 你应该能看到新技能被加载

## 常见问题

### Q: 连接不到Ollama

**A:** 确保:
- Ollama服务正在运行 (`ollama serve`)
- Base URL正确 (`http://localhost:11434`)
- 防火墙没有阻止连接
- 端口11434没有被占用

### Q: 任务一直处于Pending状态

**A:** 检查:
- Agent的Config是否正确设置
- LLM API是否可以访问
- Unity Console中的错误信息

### Q: 技能没有被加载

**A:** 确保:
- 技能文件在 `Resources/Skills` 文件夹中
- 技能类继承自 `Skill` 基类
- 技能没有在Config中被禁用

## 下一步

现在你已经成功设置了AI Agent，可以：

1. 阅读[完整文档](../README.md)了解更多功能
2. 查看[示例](Examples.md)学习高级用法
3. 创建[自定义技能](API.md#skill-class-abstract)
4. 集成[MCP服务器](Architecture.md#5-mcp-client)

## 示例命令

尝试这些命令来体验AI Agent的功能：

```
# 场景操作
创建一个名为Player的游戏对象
查找场景中所有的Light组件
删除所有名字包含Test的游戏对象

# 信息查询
列出场景中所有的游戏对象
显示当前场景的统计信息
查找所有使用某个材质的对象

# 批量操作
将所有灯光的强度设置为2
给所有游戏对象添加刚体组件
优化场景中的渲染设置
```

## 获取帮助

如果遇到问题：

1. 查看[故障排除指南](../README.md#故障排除)
2. 搜索[GitHub Issues](https://github.com/zoucdr/unity3d-agent/issues)
3. 创建新的Issue描述你的问题

祝你使用愉快！🚀

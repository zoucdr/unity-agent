# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-03-02

### Added
- Initial release of Unity3D AI Agent plugin
- Core AI Agent system with task planning and execution
- Support for multiple LLM providers:
  - Ollama (local model support)
  - OpenAI API
  - Anthropic Claude
- Skills system with context-aware skill selection
- MCP (Model Context Protocol) client support
- Unity Editor window with:
  - Command input interface
  - Task graph visualization
  - Real-time progress tracking
  - Configuration management
- Configuration system via ScriptableObjects
- Support for parallel and serial task execution
- Task dependency management
- Comprehensive documentation and examples

### Features
- Multi-task parallel and serial execution
- Dynamic skill loading from multiple directories
- Skill toggle control
- Context-based skill selection
- MCP tools, resources, and prompts integration
- Event system for task lifecycle monitoring
- Detailed logging and error handling

### Documentation
- Complete README with setup instructions
- Architecture documentation
- Usage examples
- API reference
- Troubleshooting guide

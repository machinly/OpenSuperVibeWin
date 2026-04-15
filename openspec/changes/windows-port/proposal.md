## Why

OpenSuperVibe 目前仅支持 macOS，使用 Swift/AppKit/SwiftUI 构建。将其移植到 Windows 平台可以覆盖更广泛的用户群体。Windows 版本需要从 Swift 生态完全切换到 Windows 原生技术栈，同时保持核心功能和 cyberpunk UI 风格不变。

## What Changes

- 新建 Windows 项目，技术栈为 C#/WPF/.NET 8（macOS Swift 代码保持不变，仅作参考）
- 音频录制使用 NAudio WasapiCapture 替代 AVFoundation/AVAudioEngine
- 全局热键使用 Win32 低级键盘钩子（WH_KEYBOARD_LL）替代 CGEventTap
- 系统托盘（NotifyIcon）替代 macOS 菜单栏（NSStatusBar）
- 剪贴板写入 + SendInput 模拟 Ctrl+V 替代 NSPasteboard + CGEvent
- 浮动覆盖窗口使用 WPF 透明无边框 Topmost 窗口替代 NSPanel
- STT 引擎内嵌：使用 Whisper.net（whisper.cpp 的 C# 绑定）直接集成，去掉 Python 子进程桥接
- 保留 LLM API 调用逻辑（Anthropic Claude / Google Gemini）
- 保留 cyberpunk UI 设计语言（霓虹色、扫描线效果、波形动画）

## Capabilities

### New Capabilities
- `win-system-tray`: Windows 系统托盘图标、右键菜单、设置界面
- `win-audio-capture`: Windows 音频录制（WASAPI/NAudio），16kHz mono PCM 重采样
- `win-global-hotkeys`: Windows 全局热键注册与监听（Right Option → Right Alt 映射）
- `win-overlay-hud`: Windows 浮动透明覆盖窗口，cyberpunk HUD 渲染
- `win-clipboard-paste`: Windows 剪贴板写入与 SendInput 模拟 Ctrl+V 粘贴
- `win-stt-embedded`: 内嵌 Whisper.net STT 引擎，支持 GGML 模型选择和自动下载
- `win-llm-service`: HTTP 客户端调用 Claude/Gemini API（从 Swift async/await 迁移）
- `win-config`: Windows 配置持久化（%APPDATA%\SuperVibe\config.json）

### Modified Capabilities
<!-- 这是全新平台移植，原有 macOS 代码保持不变，无修改现有 capability -->

## Impact

- **代码**: 需要创建全新的 Windows 项目，原有 Swift 代码作为参考但不直接复用
- **构建系统**: 从 SPM/Makefile 切换到 MSBuild/.csproj（如选 C#）或 CMake（如选 C++）
- **依赖**: 新增 Windows 平台依赖（NAudio、WPF/WinUI、HTTP 客户端库等）
- **STT 引擎**: mlx-audio 仅支持 Apple Silicon，Windows 版使用 Whisper.net（whisper.cpp C# 绑定）内嵌，无需 Python
- **模型**: 从 VibeVoice MLX 模型切换到 Whisper GGML 模型（tiny/base/small/medium/large）
- **配置路径**: 从 `~/.SuperVibe/` 迁移到 `%APPDATA%\SuperVibe\`
- **热键映射**: macOS Right Option 映射为 Windows Right Alt；Option+/ 映射为 Alt+/

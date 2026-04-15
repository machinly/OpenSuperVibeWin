# OpenSuperVibe

[English](README.md)

OpenSuperVibe 是一个开源的语音转文字应用，支持可选翻译功能。对着麦克风说话，转录（或翻译）后的文字会自动粘贴到当前应用。

支持 **Windows** 和 **macOS** 双平台。

## 功能

- **本地语音转录** — 按下热键，说话，文字自动输入
- **文本润色（Polish）** — LLM 自动修正语法、标点和口语痕迹
- **翻译模式** — 支持中/英/日/韩/法/西/德 7 种语言互译
- **双 LLM 支持** — Claude (Anthropic) 和 Gemini (Google)
- **赛博朋克浮动 HUD** — 霓虹色波形动画、扫描线效果
- **全局热键** — 任何应用中都能触发，无需切换窗口

## Windows 版

使用 C# / WPF / .NET 8 构建。STT 引擎为内嵌的 [Whisper.net](https://github.com/sandrohanea/whisper.net)，无需安装 Python。

### 热键

| 快捷键 | 功能 |
|---|---|
| **F9** | 开始/停止转录 |
| **Shift+F9** | 开始/停止翻译 |
| **ESC** | 取消当前会话 |

### 快速开始

1. 从 [Releases](https://github.com/machinly/OpenSuperVibeWin/releases) 下载 `SuperVibe-v0.1.0-win-x64.zip`
2. 解压后运行 `SuperVibe.exe`
3. 右键系统托盘图标，设置 API Key 和翻译语言
4. 按 F9 开始录音，再按 F9 停止并转录粘贴
5. 首次使用会自动下载 Whisper 模型（约 466MB）

### 从源码构建

```bash
dotnet build SuperVibe/SuperVibe.csproj -c Release
dotnet run --project SuperVibe -c Release
```

需要 .NET 8 SDK。

### 架构

| 文件 | 职责 |
|---|---|
| `App.xaml.cs` | 系统托盘、菜单、应用生命周期 |
| `AppState.cs` | 核心状态机、管道编排 |
| `HotkeyManager.cs` | 全局热键（WH_KEYBOARD_LL 低级键盘钩子） |
| `OverlayWindow.xaml` | 赛博朋克浮动 HUD 覆盖窗口 |
| `AudioRecorder.cs` | WASAPI 麦克风录音、16kHz 单声道重采样 |
| `WhisperSttService.cs` | 内嵌 Whisper.net 语音识别引擎 |
| `LlmService.cs` | Claude/Gemini API 调用（润色和翻译） |
| `ClipboardPasteService.cs` | 剪贴板写入 + SendInput 模拟 Ctrl+V |
| `ConfigService.cs` | 配置持久化（%APPDATA%\SuperVibe） |

## macOS 版

使用 Swift / SwiftUI / AppKit 构建。STT 引擎为 [mlx-audio](https://github.com/ml-explore/mlx-audio) VibeVoice（需要 Apple Silicon）。

源代码在 [`macos/`](macos/) 目录。

### 热键

| 快捷键 | 功能 |
|---|---|
| **Right Option**（按下松开） | 开始/停止转录 |
| **Right Option + /** | 开始/停止翻译 |
| **ESC** | 取消当前会话 |

### 系统要求

- macOS 14.0+，推荐 Apple Silicon
- 通过 pipx 安装 `mlx-audio`：`brew install pipx && pipx install mlx-audio`
- 需要麦克风和辅助功能权限

### 从源码构建

```bash
cd macos
swift build
swift run
```

## 工作原理

1. 按下热键开始录音
2. 麦克风音频实时捕获到内存
3. 停止后本地转录（Windows 用 Whisper，macOS 用 VibeVoice）
4. 可选 LLM 后处理：润色或翻译
5. 最终文字粘贴到当前前台应用

## 许可证

MIT。见 `LICENSE`。

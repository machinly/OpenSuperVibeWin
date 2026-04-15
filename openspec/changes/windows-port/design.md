## Context

OpenSuperVibe 是一个 macOS 菜单栏应用，使用 Swift/AppKit/SwiftUI 构建，深度依赖 Apple 平台 API（AVFoundation 音频、CGEventTap 热键、NSPanel 覆盖窗口、NSPasteboard 剪贴板）。现需移植到 Windows 平台，核心管道 **热键 → 录音 → 转录 → (可选 LLM) → 粘贴** 保持不变，但所有平台交互层需要重写。

当前 macOS 版本约 2200 行 Swift 代码，分布在 11 个文件中。macOS 版通过 Python 子进程桥接 mlx-audio 实现 STT，Windows 版将改用 Whisper.net 内嵌方案，不再依赖 Python。

## Goals / Non-Goals

**Goals:**
- 在 Windows 10/11 上实现与 macOS 版本功能对等的语音转文字应用
- 保持 cyberpunk UI 视觉风格（霓虹色、扫描线、波形动画）
- 使用 Whisper.net 内嵌 STT，复用 LLM API 调用逻辑
- 系统托盘常驻，全局热键随时触发
- 配置持久化到 `%APPDATA%\SuperVibe\config.json`

**Non-Goals:**
- 跨平台统一代码库（macOS 和 Windows 各自维护）
- 移动平台支持（iOS/Android）
- 自定义 STT 模型训练或微调
- 安装程序/自动更新机制（首版手动运行）
- macOS 版本的任何改动

## Decisions

### 1. 技术栈：C# + WPF（.NET 8）

**选择**: C# / WPF / .NET 8
**替代方案**:
- C++ / Win32：性能最优但开发效率低，UI 实现复杂
- Rust / egui：生态尚不成熟，系统 API 绑定需要大量 unsafe 代码
- Electron：可实现 cyberpunk UI 但内存占用大，系统集成弱

**理由**: WPF 提供强大的 UI 自定义能力（适合 cyberpunk 风格），.NET 8 有成熟的系统 API 互操作（P/Invoke），NAudio 提供完善的音频支持，HttpClient 内置异步 HTTP。开发效率和系统集成的最佳平衡。

### 2. 音频录制：NAudio + WASAPI

**选择**: NAudio 库的 WasapiCapture
**替代方案**:
- Windows.Media.Capture (UWP API)：需要额外的 capability 声明，部署复杂
- DirectSound：已过时

**理由**: NAudio 是 .NET 生态最成熟的音频库，WasapiCapture 提供低延迟录制，内置重采样支持（WdlResamplingSampleProvider 可直接转换到 16kHz mono）。

### 3. 全局热键：SetWindowsHookEx 低级键盘钩子

**选择**: P/Invoke 调用 SetWindowsHookEx (WH_KEYBOARD_LL)
**替代方案**:
- RegisterHotKey：更简单但 **无法区分左右 Alt 键**（MOD_ALT 同时匹配 Left Alt 和 Right Alt），不满足需求
- RawInput API：过于底层，需要自行管理设备输入

**理由**: 低级键盘钩子可以通过 `KBDLLHOOKSTRUCT.vkCode`（VK_RMENU = 0xA5 为 Right Alt，VK_LMENU = 0xA4 为 Left Alt）和 `flags` 中的 `LLKHF_EXTENDED` 标志精确区分左右 Alt。回调中检测 Right Alt 按下/释放切换录音，Alt+/ 组合触发翻译模式，ESC 取消会话。钩子不需要管理员权限，但需要在有消息循环的线程上安装。

### 4. STT 引擎：Whisper.net 内嵌（替代 mlx-audio + Python 桥接）

**选择**: Whisper.net（whisper.cpp 的 C# 绑定），通过 NuGet 包直接集成
**替代方案**:
- faster-whisper (Python)：性能好但需要 Python 运行时和子进程管理，增加用户安装复杂度
- OpenAI Whisper 原版 (Python)：速度慢，显存占用高，同样需要 Python
- Azure Speech SDK：需要云服务，违背本地优先原则

**理由**: Whisper.net 将 whisper.cpp 编译为 native library 并提供 C# API，完全消除 Python 依赖。NuGet 包 `Whisper.net.Runtime.Cuda`（NVIDIA GPU）和 `Whisper.net.Runtime.Cpu`（CPU fallback）提供开箱即用的硬件加速。使用 GGML 格式 Whisper 模型（tiny ~75MB / base ~142MB / small ~466MB / medium ~1.5GB / large ~3GB），首次使用时可从 Hugging Face 自动下载。这彻底简化了部署——单个 .exe（self-contained）即可运行，无需用户安装任何外部依赖。

### 5. 覆盖窗口：WPF 透明无边框窗口

**选择**: WPF Window + AllowsTransparency + Topmost
**替代方案**:
- WinUI 3：仍不成熟，透明窗口支持差
- Win32 Layered Window：底层控制力强但 UI 绘制复杂

**理由**: WPF 原生支持透明窗口、动画、自定义渲染，可以直接用 XAML 和 Storyboard 实现 cyberpunk 视觉效果（霓虹发光、扫描线着色器、波形条动画）。

### 6. 项目结构

```
SuperVibe.Windows/
├── SuperVibe.sln
├── SuperVibe/
│   ├── SuperVibe.csproj
│   ├── App.xaml / App.xaml.cs          # 应用入口、系统托盘
│   ├── Models/
│   │   ├── SessionStage.cs             # 状态枚举
│   │   ├── AppConfig.cs                # 配置模型
│   │   └── LlmModels.cs               # LLM 模型定义
│   ├── Services/
│   │   ├── AppState.cs                 # 核心状态机
│   │   ├── AudioRecorder.cs            # NAudio 录音
│   │   ├── WhisperSttService.cs        # Whisper.net 内嵌 STT
│   │   ├── LlmService.cs              # Claude/Gemini API
│   │   ├── HotkeyManager.cs           # 全局热键
│   │   ├── ClipboardPasteService.cs    # 剪贴板 + SendInput
│   │   └── ConfigService.cs           # 配置读写
│   ├── Views/
│   │   ├── OverlayWindow.xaml          # 浮动 HUD
│   │   ├── RecordingHud.xaml           # 波形条组件
│   │   └── CyberCard.xaml             # 霓虹文本卡片
│   └── Resources/
│       └── (Whisper GGML 模型由用户下载或首次运行自动获取)
└── README.md
```

## Risks / Trade-offs

- **[UI 保真度]** WPF 实现 cyberpunk 效果可能与 SwiftUI 版本有视觉差异 → 接受合理差异，保持设计语言一致即可
- **[WPF 透明窗口性能]** WPF 的 AllowsTransparency=true 使用软件渲染，在高 DPI 或大面积透明区域可能有性能问题 → 覆盖窗口保持较小尺寸，必要时可降级为半透明深色背景而非完全透明
- **[STT 性能]** Whisper.net CPU 模式在大模型（medium/large）上可能较慢 → 默认推荐 small 模型，有 NVIDIA GPU 的用户可启用 CUDA runtime 使用更大模型
- **[全局热键冲突]** Right Alt 在某些键盘布局（德语、法语等）中作为 AltGr 使用 → 提供热键自定义配置作为后续改进
- **[模型下载]** GGML 模型文件较大（small ~466MB），首次下载可能耗时 → 提供下载进度提示，支持手动放置模型文件
- **[权限]** SendInput 模拟按键在某些安全软件下可能被拦截 → 文档说明白名单配置

## Open Questions

- Windows 最低支持版本是 Windows 10 1903 还是仅 Windows 11？（影响部分 API 可用性）
- 首版是否需要 self-contained 单文件部署（.exe 内嵌 .NET runtime），还是要求用户安装 .NET 8 Runtime？
- Whisper GGML 模型存放路径：`%APPDATA%\SuperVibe\models\` 还是与 exe 同目录？
- Windows 项目放在当前仓库（OpenSuperVibeWin）根目录还是子目录中？仓库目前包含 macOS Swift 代码

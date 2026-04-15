## 1. 项目脚手架

- [x] 1.1 创建 `SuperVibe.sln` 和 `SuperVibe.csproj`（.NET 8, WPF, OutputType=WinExe）
- [x] 1.2 添加 NuGet 依赖：NAudio, Whisper.net, Whisper.net.Runtime, Whisper.net.Runtime.Cuda.Windows
- [x] 1.3 创建目录结构：Models/, Services/, Views/, Resources/
- [x] 1.4 创建 App.xaml / App.xaml.cs 入口，配置为无主窗口启动（ShutdownMode=OnExplicitShutdown）
- [x] 1.5 更新 .gitignore 添加 .NET 排除项（bin/, obj/, *.user, .vs/）
- [x] 1.6 创建或获取波形风格 .ico 图标文件（至少 16x16、32x32、48x48 尺寸），放入 Resources/

## 2. 配置与数据模型（win-config）

- [x] 2.1 创建 Models/SessionStage.cs — 枚举：Idle, Recording, Recognizing, Translating, Done, Error
- [x] 2.2 创建 Models/LlmModels.cs — Claude 和 Gemini 模型列表定义（与 macOS 版保持一致）
- [x] 2.3 创建 Models/AppConfig.cs — 配置数据模型（translation_language, stt_model, llm_api_key, gemini_api_key, llm_provider, polish_enabled, llm_model, gemini_model）
- [x] 2.4 创建 Services/ConfigService.cs — 读写 `%APPDATA%\SuperVibe\config.json`，自动创建目录，未知字段保留（使用 JsonExtensionData）
- [x] 2.5 实现默认值逻辑：stt_model 默认 "small"，polish_enabled 默认 true，llm_provider 默认 "claude"

## 3. 核心状态机骨架（AppState）

- [x] 3.1 创建 Services/AppState.cs — SessionStage 属性、状态转换方法签名（StartSession/StopSession/Cancel），service 依赖注入占位
- [x] 3.2 实现 INotifyPropertyChanged，暴露 Stage、StatusText、AsrText、GptText 供 UI 绑定
- [x] 3.3 集成 ConfigService，设置变更时自动保存

## 4. 音频录制（win-audio-capture）

- [x] 4.1 创建 Services/AudioRecorder.cs — WasapiCapture 录音，累积 PCM 数据到内存缓冲区
- [x] 4.2 实现重采样：使用 WdlResamplingSampleProvider 将任意输入格式转换为 16kHz mono
- [x] 4.3 实现实时 RMS 音量计算（归一化 0.0–1.0），通过回调暴露给 UI 波形动画
- [x] 4.4 实现 GetBufferAsFloat32() 方法：录音停止后将累积的 Int16 PCM 转换为 float32 数组供 Whisper.net 直接消费

## 5. Whisper STT 引擎（win-stt-embedded）

- [x] 5.1 创建 Services/WhisperSttService.cs — Whisper.net 处理器封装，支持模型加载和复用
- [x] 5.2 实现 GGML 模型管理：检查 `%APPDATA%\SuperVibe\models\` 中模型文件是否存在
- [x] 5.3 实现模型自动下载：从 Hugging Face (ggerganov/whisper.cpp) 下载缺失模型，带进度回调
- [x] 5.4 实现音频转录：接收 float32 音频缓冲区，返回转录文本
- [x] 5.5 实现语言参数配置：将 WhisperProcessor 的 Language 参数暴露为可配置项，提升中文/日文等非英语识别准确率
- [x] 5.6 实现 CUDA/CPU 自动检测与 fallback
- [x] 5.7 实现模型切换：dispose 旧 processor，加载新模型
- [x] 5.8 实现 IDisposable，应用退出时释放 native 资源

## 6. LLM 服务（win-llm-service）

- [x] 6.1 创建 Services/LlmService.cs — HttpClient 异步调用 Anthropic Claude API（Messages API）
- [x] 6.2 实现 Gemini API 调用（generateContent endpoint）
- [x] 6.3 实现 polish 功能：发送语法/标点修正 prompt
- [x] 6.4 实现 translation 功能：发送翻译 prompt（支持 7 种目标语言）
- [x] 6.5 实现 refusal 检测：识别 LLM 拒绝回复的标记，fallback 到原始 ASR 文本
- [x] 6.6 实现 provider/model 运行时切换

## 7. 全局热键（win-global-hotkeys）

- [x] 7.1 创建 Services/HotkeyManager.cs — P/Invoke SetWindowsHookEx 在 WPF Dispatcher 线程上安装 WH_KEYBOARD_LL 钩子
- [x] 7.2 实现 Right Alt（VK_RMENU 0xA5）按下/释放检测，区分 Left Alt（VK_LMENU 0xA4）
- [x] 7.3 实现 Right Alt + /（VK_OEM_2）组合键检测：跟踪 Alt 按下状态
- [x] 7.4 实现 ESC（VK_ESCAPE）取消当前会话，idle 状态下 pass-through

## 8. 剪贴板与粘贴（win-clipboard-paste）

- [x] 8.1 创建 Services/ClipboardPasteService.cs — Clipboard.SetText 写入 Unicode 文本
- [x] 8.2 实现 SendInput P/Invoke 模拟 Ctrl+V 按键
- [x] 8.3 实现可选 Enter 键模拟

## 9. 系统托盘（win-system-tray）

- [x] 9.1 在 App.xaml.cs 中创建 NotifyIcon 系统托盘图标（使用 1.6 准备的 .ico）
- [x] 9.2 实现右键菜单：Start/Stop Recording、Translation Language 子菜单、STT Model 选择、LLM Provider/Model 选择、Polish 开关、Quit
- [x] 9.3 实现托盘图标状态切换：空闲 vs 录音中的视觉区分
- [x] 9.4 实现 API Key 输入对话框（Anthropic + Gemini key、provider 选择、model 选择）

## 10. 浮动覆盖 HUD（win-overlay-hud）

- [x] 10.1 创建 Views/OverlayWindow.xaml — WPF 窗口：WindowStyle=None, AllowsTransparency=True, Topmost=True, ShowInTaskbar=False
- [x] 10.2 设置 WS_EX_NOACTIVATE 窗口样式（通过 WindowInteropHelper），防止抢夺焦点
- [x] 10.3 实现屏幕底部居中定位（基于 SystemParameters.PrimaryScreenWidth/Height）
- [x] 10.4 创建波形条动画 — 12 根波形条，绑定 RMS 音量数据（内嵌在 OverlayWindow 中）
- [x] 10.5 实现霓虹边框、扫描线效果、半透明深色背景（CyberCard 风格内嵌）
- [x] 10.6 实现双色主题切换：转录模式 cyan、翻译模式 magenta
- [x] 10.7 实现文本结果卡片：单卡（仅转录）和双卡（ASR + 翻译）
- [x] 10.8 实现处理中动画：闪烁点（Recognizing/Translating）
- [x] 10.9 实现错误状态显示及自动隐藏超时
- [x] 10.10 实现会话结束后短暂展示结果再隐藏

## 11. 管道集成与端到端测试

- [x] 11.1 在 AppState 中串联完整管道：热键 → AudioRecorder → WhisperStt → (LlmService) → ClipboardPaste
- [x] 11.2 将 AppState 状态变更连接到 OverlayWindow 显示/隐藏和内容更新
- [x] 11.3 连接 HotkeyManager 事件到 AppState 的 StartSession/StopSession/Cancel
- [x] 11.4 验证粘贴目标为前台应用而非 overlay 窗口（配合 10.2 WS_EX_NOACTIVATE）
- [ ] 11.5 手动端到端测试：按 Right Alt 录音 → 松开 → 转录 → 粘贴到记事本
- [ ] 11.6 手动端到端测试：按 Right Alt+/ 录音 → 松开 → 转录 → 翻译 → 粘贴
- [ ] 11.7 手动端到端测试：录音中按 ESC → 取消 → 回到 idle

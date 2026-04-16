## 1. ISttEngine 接口与 Whisper 适配

- [x] 1.1 创建 Services/ISttEngine.cs — 定义接口：Name, IsAvailable, EnsureModelLoadedAsync, TranscribeAsync, Dispose
- [x] 1.2 修改 WhisperSttService 实现 ISttEngine 接口（IsAvailable 始终返回 true）
- [x] 1.3 将 WriteWavToStream 提取为 Services/AudioUtils.cs 静态工具方法，供两个引擎共用

## 2. VibeVoice 子进程桥接

- [x] 2.1 创建 Resources/vibevoice_server.py — 持久化 Python server：使用 VibeVoiceASRProcessor + VibeVoiceASRForConditionalGeneration API 加载模型，stdin 读 WAV 路径，解析结构化输出提取纯文本（去除时间戳和说话人标签），stdout 写 JSON 结果
- [x] 2.2 创建 Services/VibeVoiceSttService.cs — 实现 ISttEngine，管理 Python 子进程生命周期（启动、READY 检测、停止）。stderr 重定向到 Debug 输出避免干扰 stdout JSON 读取
- [x] 2.3 实现 Python 环境检测：查找 python/python3，验证 vibevoice 包可导入（启动时后台异步检测，结果缓存到 IsAvailable 属性）
- [x] 2.4 实现 TranscribeAsync：将 float32 音频写入临时 WAV 文件，通过 stdin 发送路径，从 stdout 读取 JSON 结果
- [x] 2.5 实现 IDisposable：终止子进程，清理临时文件
- [x] 2.6 在 csproj 中添加 vibevoice_server.py 为 Content 资源（CopyToOutputDirectory）

## 3. AppState 引擎切换

- [x] 3.1 AppConfig 新增 SttEngine 字段（默认 "whisper"），ConfigService 兼容缺失字段
- [x] 3.2 AppState 新增 ISttEngine 引用，替换直接的 WhisperSttService 引用。WhisperSttService 构造时传入 Func&lt;string&gt; 获取当前 stt_model 设置
- [x] 3.3 AppState 新增 SwitchEngine 方法：Dispose 旧引擎，创建新引擎，保存 config
- [x] 3.4 AppState.Initialize 中根据 config 初始化对应引擎（VibeVoice 不可用时回落到 Whisper）
- [x] 3.5 修改 RunTranscriptionPipeline 使用 ISttEngine 接口调用

## 4. 托盘菜单

- [x] 4.1 App.xaml.cs RebuildMenu 添加 STT Engine 子菜单（Whisper / VibeVoice）
- [x] 4.2 VibeVoice 不可用时菜单项灰显，显示"(not installed)"
- [x] 4.3 切换引擎时调用 AppState.SwitchEngine 并 RebuildMenu

## 5. 测试

- [ ] 5.1 手动测试：无 VibeVoice 环境时应用正常启动，菜单灰显
- [ ] 5.2 手动测试：有 VibeVoice 环境时切换引擎，录音 → 转录
- [ ] 5.3 手动测试：VibeVoice 子进程异常退出后回落到 Whisper

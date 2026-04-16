## Context

Windows 版 SuperVibe 使用 Whisper.net 内嵌 STT。现需添加微软 VibeVoice-ASR 作为第二个 STT 引擎选项。VibeVoice 是纯 Python 项目（7B 参数，基于 Qwen2.5-7B），需要 PyTorch + CUDA。通过子进程桥接，与 macOS 版的 VibeVoiceSTT 设计模式一致。

## Goals / Non-Goals

**Goals:**
- 引入 ISttEngine 接口统一 STT 引擎调用
- 实现 VibeVoiceSttService 通过子进程桥接 Python
- 支持在托盘菜单切换 Whisper / VibeVoice
- VibeVoice 不可用时（Python 未安装或缺少依赖）优雅降级

**Non-Goals:**
- 在 C# 内嵌 Python 解释器（Python.NET）
- 自动安装 Python 或 VibeVoice 依赖
- VibeVoice 模型选择（只支持默认的 microsoft/VibeVoice-ASR）

## Decisions

### 1. ISttEngine 接口设计

```csharp
public interface ISttEngine : IDisposable
{
    string Name { get; }
    bool IsAvailable { get; }
    Task EnsureModelLoadedAsync(CancellationToken ct = default);
    Task<string> TranscribeAsync(float[] audioBuffer, CancellationToken ct = default);
}
```

WhisperSttService 和 VibeVoiceSttService 都实现此接口。AppState 持有当前活跃的 ISttEngine 引用，根据 config 切换。

`EnsureModelLoadedAsync` 不接受 model 参数——每个引擎从自己的配置或内部状态获取模型信息（Whisper 读 config.SttModel，VibeVoice 固定使用 microsoft/VibeVoice-ASR）。这避免了两个引擎对 model 参数语义不同的问题。

**理由**: 最小化接口，两个引擎的核心需求就是"确保模型就绪"和"转录音频"。IsAvailable 用于检测引擎是否可用（VibeVoice 需要 Python 环境）。

### 2. VibeVoice 子进程桥接

采用与 macOS 版相同的**持久化子进程**模式：
- 启动时加载模型（耗时 ~30s），保持进程常驻
- 通过 stdin 发送 WAV 文件路径，stdout 接收 JSON 结果
- 格式：`{"ok": true, "text": "..."}` 或 `{"ok": false, "error": "..."}`
- 子进程启动后发送 "READY\n" 信号表示模型加载完成

**替代方案**: 每次转录启动新进程 → 模型加载耗时太长（~30s），不可行。

**理由**: 与 macOS 版一致，经过验证的设计。模型只加载一次，后续转录延迟低。

### 3. Python 环境检测

VibeVoice 需要用户手动安装：
1. Python 3.10+
2. `pip install -e .` 从 microsoft/VibeVoice 仓库
3. PyTorch + CUDA

检测逻辑：
- 查找 `python` 或 `python3` 命令
- 执行 `python -c "from vibevoice.modular.modeling_vibevoice_asr import VibeVoiceASRForConditionalGeneration; print('ok')"`
- 成功则 IsAvailable = true

**理由**: 不假设 Python 安装路径，用 PATH 查找最灵活。

### 4. 音频格式转换

VibeVoice 接受 WAV 文件路径。AudioRecorder 输出 float32 数组。需要写入临时 WAV 文件再传路径给子进程。

WhisperSttService 内部已有 WriteWavToStream 方法，提取为共享工具方法。

### 5. vibevoice_server.py

基于微软 VibeVoice 的 inference API 编写：

```python
# 启动: python vibevoice_server.py [model_path]
# 加载模型后输出 READY
# 循环读取 stdin WAV 路径，输出 JSON 结果
```

使用 `VibeVoiceASRProcessor` + `VibeVoiceASRForConditionalGeneration` API。

## Risks / Trade-offs

- **[安装复杂度]** VibeVoice 需要用户自行安装 Python + PyTorch + CUDA + vibevoice 包 → 在菜单中灰显并提示安装说明
- **[GPU 内存]** 7B 模型需要 ~16GB VRAM → 只有高端 GPU 用户能用，默认引擎仍为 Whisper
- **[子进程稳定性]** Python 子进程可能意外退出 → 检测进程状态，出错时回落到 Whisper
- **[首次加载慢]** 模型加载约 30 秒 → 显示加载进度提示
- **[模型下载]** 首次使用需从 Hugging Face 下载 VibeVoice-ASR + Qwen2.5-7B（共约 20GB+）→ 由 transformers 库自动管理，缓存到 ~/.cache/huggingface

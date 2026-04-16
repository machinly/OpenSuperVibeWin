## Why

Windows 版目前只有 Whisper.net 一种 STT 引擎。微软开源了 VibeVoice-ASR（7B 参数），支持 50+ 语言、60 分钟长音频、说话人识别，转录质量显著优于 Whisper small/medium。添加 VibeVoice 作为可选 STT 引擎，通过 Python 子进程桥接，用户可在托盘菜单切换引擎。

## What Changes

- 新增 VibeVoiceSttService：通过 Python 子进程运行 vibevoice_server.py，与 macOS 版的子进程桥接模式一致
- 新增 Resources/vibevoice_server.py：持久化 Python server，启动时加载模型，stdin/stdout 通信
- 引入 ISttEngine 接口，WhisperSttService 和 VibeVoiceSttService 都实现它
- AppState 根据配置选择 STT 引擎，支持运行时切换
- 托盘菜单增加 STT Engine 选择（Whisper / VibeVoice），VibeVoice 不可用时灰显
- config.json 新增 `stt_engine` 字段（"whisper" 或 "vibevoice"，默认 "whisper"）

## Capabilities

### New Capabilities
- `stt-engine-switch`: STT 引擎抽象层（ISttEngine 接口）、VibeVoiceSttService 子进程实现、托盘菜单引擎选择、运行时切换

### Modified Capabilities
- `win-config`: 新增 `stt_engine` 配置字段
- `win-system-tray`: 托盘菜单增加 STT Engine 子菜单

## Impact

- **代码**: 新增 ISttEngine.cs、VibeVoiceSttService.cs；修改 WhisperSttService.cs（实现接口）、AppState.cs、AppConfig.cs、App.xaml.cs
- **资源**: 新增 Resources/vibevoice_server.py（随应用分发）
- **依赖**: 运行时可选依赖——Python 3.10+、PyTorch、vibevoice 包（`pip install -e .` 从 github.com/microsoft/VibeVoice）、NVIDIA GPU
- **配置**: config.json 新增 stt_engine 字段，向后兼容（缺失时默认 whisper）

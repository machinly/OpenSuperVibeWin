# OpenSuperVibe

[中文](README_CN.md)

OpenSuperVibe is an open-source voice-to-text app with optional translation. Speak naturally, and the transcribed (or translated) text is automatically pasted into the active application.

Available for **Windows** and **macOS**.

## Features

- **Local Voice Transcription** -- press a hotkey, speak, and the text is typed for you
- **Text Polish** -- LLM-powered grammar and punctuation cleanup
- **Optional Translation** -- translate speech into English, Chinese, Japanese, Korean, French, Spanish, or German
- **Dual LLM Support** -- Claude (Anthropic) and Gemini (Google)
- **Cyberpunk Overlay UI** -- floating HUD with animated waveform bars, neon color coding, and scan-line aesthetics
- **Global Hotkeys** -- works system-wide, no need to focus the app

## Windows

Built with C# / WPF / .NET 8. STT via embedded [Whisper.net](https://github.com/sandrohanea/whisper.net) (no Python required).

### Hotkeys

| Shortcut | Action |
|---|---|
| **F9** | Start/stop transcription |
| **Shift+F9** | Start/stop translation |
| **ESC** | Cancel current session |

### Quick Start

1. Download `SuperVibe-v0.1.0-win-x64.zip` from [Releases](https://github.com/machinly/OpenSuperVibeWin/releases)
2. Extract and run `SuperVibe.exe`
3. Right-click tray icon to set API Key and translation language
4. Press F9 to record, press F9 again to transcribe and paste
5. First use downloads Whisper model (~466MB) automatically

### Build from Source

```bash
dotnet build SuperVibe/SuperVibe.csproj -c Release
dotnet run --project SuperVibe -c Release
```

Requires .NET 8 SDK.

### Architecture

| File | Role |
|---|---|
| `App.xaml.cs` | System tray, menu, app lifecycle |
| `AppState.cs` | Core state machine, pipeline orchestration |
| `HotkeyManager.cs` | Global hotkeys via WH_KEYBOARD_LL |
| `OverlayWindow.xaml` | Floating cyberpunk HUD overlay |
| `AudioRecorder.cs` | WASAPI microphone capture, 16kHz mono resampling |
| `WhisperSttService.cs` | Embedded Whisper.net STT engine |
| `LlmService.cs` | Claude/Gemini API for polish and translation |
| `ClipboardPasteService.cs` | Clipboard + SendInput Ctrl+V paste |
| `ConfigService.cs` | Config persistence (%APPDATA%\SuperVibe) |

## macOS

Built with Swift / SwiftUI / AppKit. STT via [mlx-audio](https://github.com/ml-explore/mlx-audio) VibeVoice (Apple Silicon).

Source code is in the [`macos/`](macos/) directory.

### Hotkeys

| Shortcut | Action |
|---|---|
| **Right Option** (press & release) | Start/stop transcription |
| **Right Option + /** | Start/stop translation |
| **ESC** | Cancel current session |

### Requirements

- macOS 14.0+, Apple Silicon recommended
- `mlx-audio` via pipx: `brew install pipx && pipx install mlx-audio`
- Microphone + Accessibility permissions

### Build from Source

```bash
cd macos
swift build
swift run
```

## How It Works

1. Press hotkey to start recording
2. Audio captured from microphone, kept in memory
3. On stop, audio is transcribed locally (Whisper on Windows, VibeVoice on macOS)
4. Optional LLM post-processing: polish or translate
5. Final text pasted into the frontmost application

## License

MIT. See `LICENSE`.

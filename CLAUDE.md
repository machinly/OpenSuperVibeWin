# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OpenSuperVibe is a cross-platform voice-to-text app with optional translation. The project name is **OpenSuperVibe** but the executable and app bundle are named **SuperVibe**.

This repo contains both the **Windows** and **macOS** versions:
- `SuperVibe/` — Windows (C# / WPF / .NET 8)
- `macos/` — macOS (Swift / SwiftUI / AppKit)

## Windows Version

- **Language**: C# / .NET 8 / WPF
- **Platform**: Windows 10/11 x64
- **STT Engine**: Whisper.net (embedded, no Python)
- **License**: MIT

### Build Commands

```bash
dotnet build SuperVibe/SuperVibe.csproj -c Release
dotnet run --project SuperVibe -c Release
dotnet publish SuperVibe/SuperVibe.csproj -c Release --self-contained -r win-x64
```

### Architecture

The app follows a pipeline: **Hotkey → Record → Transcribe → (Optional LLM) → Paste**.

#### State Machine (`Services/AppState.cs`)

`AppState` is the central orchestrator implementing `INotifyPropertyChanged`. It drives a `SessionStage` enum through: `Idle → Recording → Recognizing → Translating → Done → Error`. Uses events (`RecordingChanged`, `ConfigChanged`, `OverlayUpdate`) to propagate state to UI. Config persisted to `%APPDATA%\SuperVibe\config.json`.

#### Key Components

- **`HotkeyManager`** — Global hotkey detection via `SetWindowsHookEx` (WH_KEYBOARD_LL). F9 starts/stops transcription; Shift+F9 triggers translation mode; ESC cancels.
- **`AudioRecorder`** — Captures microphone audio via NAudio WasapiCapture, resamples to 16kHz mono float32 using `WdlResamplingSampleProvider`. Thread-safe PCM buffer.
- **`WhisperSttService`** — Embedded Whisper.net STT engine. Manages GGML model download, loading, and transcription. Supports CUDA GPU acceleration.
- **`LlmService`** — Async HTTP client for Claude (Anthropic) and Gemini (Google) APIs. Polish and translation with refusal detection.
- **`OverlayWindow`** — WPF transparent topmost window with WS_EX_NOACTIVATE. Cyberpunk HUD with 12-bar waveform animation, neon themes (cyan/magenta), dot animations.
- **`ClipboardPasteService`** — Writes text to clipboard and simulates Ctrl+V via `SendInput` P/Invoke.
- **`ConfigService`** — JSON config persistence with `JsonExtensionData` for unknown field preservation.

### Conventions

- All UI operations must run on the WPF Dispatcher thread. Use `BeginInvoke` (not `Invoke`) for hotkey callbacks to avoid blocking the keyboard hook.
- `AppState` owns all services and manages their lifecycle via `IDisposable`.
- No test target. Manual testing via `dotnet run`.

## macOS Version

Source code in `macos/` directory.

- **Language**: Swift 5.9+, SwiftUI + AppKit hybrid
- **Platform**: macOS 14.0+, Apple Silicon recommended
- **Build system**: Swift Package Manager (SPM)

### Build Commands

```bash
cd macos
make build       # swift build -c release
make run         # Release build + run
make debug       # Debug build + run
```

### Key Components

- **`HotkeyManager`** — Right Option starts/stops; Option+/ triggers translation; ESC cancels.
- **`AudioRecorder`** — AVFoundation capture, resamples to 16kHz mono PCM.
- **`VibeVoiceSTT`** — Python subprocess running `mlx-audio` for on-device ASR.
- **`LLMService`** — Claude/Gemini API with refusal detection.
- **`OverlayPanel`** — Floating SwiftUI overlay in NSPanel with cyberpunk HUD.
- **`PasteService`** — NSPasteboard + CGEvent Cmd+V paste.

### External Dependency

Requires `mlx-audio` via pipx: `~/.local/pipx/venvs/mlx-audio/bin/python3`.

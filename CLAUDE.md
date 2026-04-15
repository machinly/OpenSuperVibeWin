# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OpenSuperVibe is a macOS menu bar app for local voice-to-text input with optional translation. The project name is **OpenSuperVibe** but the Swift target, executable, and app bundle are named **SuperVibe**.

- **Language**: Swift 5.9+, SwiftUI + AppKit hybrid
- **Platform**: macOS 14.0+, Apple Silicon recommended
- **Build system**: Swift Package Manager (SPM)
- **License**: MIT

## Build Commands

```bash
make build       # swift build -c release
make run         # Release build + run
make debug       # Debug build + run
make bundle      # Build + create codesigned SuperVibe.app bundle
make clean       # Remove .build/ and SuperVibe.app/
```

Direct SPM commands also work: `swift build`, `swift run`, `open Package.swift` (Xcode).

There are no tests or CI/CD pipelines in this repository.

## Architecture

The app follows a pipeline: **Hotkey → Record → Transcribe → (Optional LLM) → Paste**.

### State Machine (`AppState.swift`)

`AppState` is the central orchestrator. It drives a `SessionStage` enum through: `idle → recording → recognizing → translating → done → error`. It uses property observers (`didSet`) and callbacks (`onRecordingChanged`, `onConfigChanged`, `onAlert`) to propagate state to UI. Config is persisted to `~/.SuperVibe/config.json`.

### Key Components

- **`HotkeyManager`** — Global hotkey detection via `NSEvent` monitors + `CGEventTap`. Right Option starts/stops; Option+/ triggers translation mode; ESC cancels. The event tap suppresses the macOS ÷ character on Option+/.
- **`AudioRecorder`** — Captures microphone audio via AVFoundation, resamples to 16kHz mono PCM (Int16) using `AVAudioConverter`. Uses a data lock for thread safety.
- **`VibeVoiceSTT`** — Manages a **persistent Python subprocess** running `mlx-audio` for on-device ASR. PCM is written to a temp WAV file and sent to the Python server (`Resources/vibevoice_server.py`). Returns JSON.
- **`LLMService`** — Async API abstraction supporting Claude (Anthropic) and Gemini (Google). Used for two purposes: polishing (grammar/punctuation fix) and translation. Includes refusal detection.
- **`OverlayPanel`** — Floating SwiftUI overlay rendered in an `NSPanel`. Contains the cyberpunk HUD: animated waveform bars, neon text cards, scan-line effects. Cyan = transcription mode, magenta = translation mode.
- **`PasteService`** — Writes text to `NSPasteboard` and simulates Cmd+V via `CGEvent` at HID tap level.

### External Dependency

The app bridges to Python for local STT. It expects `mlx-audio` installed via pipx at `~/.local/pipx/venvs/mlx-audio/bin/python3`. The default model is `mlx-community/VibeVoice-ASR-4bit` (~5GB).

## Conventions

- `AppState` is marked `@unchecked Sendable` — be careful with thread safety when modifying shared state.
- Modern Swift concurrency (async/await) is used for LLM calls and background tasks.
- The app has no test target. Manual testing via `make run` or `make debug`.

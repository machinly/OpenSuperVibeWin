## ADDED Requirements

### Requirement: Whisper.net integrated STT
The application SHALL use Whisper.net (C# bindings for whisper.cpp) as an in-process STT engine. The engine SHALL be initialized with a GGML model file and process WAV audio buffers directly without any external subprocess.

#### Scenario: First transcription triggers model load
- **WHEN** the first transcription is requested
- **THEN** the Whisper.net processor SHALL be initialized with the configured GGML model file

#### Scenario: Subsequent transcriptions reuse processor
- **WHEN** a transcription is requested after the processor is already loaded
- **THEN** the existing processor SHALL be reused without reloading the model

### Requirement: GGML model management
The application SHALL support Whisper GGML models stored at `%APPDATA%\SuperVibe\models\`. Available model sizes: tiny (~75MB), base (~142MB), small (~466MB), medium (~1.5GB), large (~3GB). The default model SHALL be `small`.

#### Scenario: Model file present
- **WHEN** the configured model file exists in the models directory
- **THEN** the engine SHALL load the model and become ready for transcription

#### Scenario: Model file missing — automatic download
- **WHEN** the configured model file does not exist
- **THEN** the application SHALL download it from Hugging Face (ggerganov/whisper.cpp), showing download progress to the user

#### Scenario: Manual model placement
- **WHEN** the user manually places a GGML model file in the models directory
- **THEN** the application SHALL detect and use it without downloading

### Requirement: Model selection via tray menu
The application SHALL allow selecting between available Whisper model sizes via the tray menu.

#### Scenario: Change model
- **WHEN** user selects a different model size from the tray menu
- **THEN** the new model SHALL be loaded (downloading if needed) and used for subsequent transcriptions. The previous processor SHALL be disposed.

### Requirement: Hardware acceleration
The application SHALL support NVIDIA CUDA acceleration via the `Whisper.net.Runtime.Cuda` NuGet package when a compatible GPU is available, falling back to CPU via `Whisper.net.Runtime.Cpu` otherwise.

#### Scenario: CUDA GPU available
- **WHEN** a compatible NVIDIA GPU and CUDA runtime are present
- **THEN** the engine SHALL use GPU acceleration for transcription

#### Scenario: No GPU available
- **WHEN** no compatible GPU is detected
- **THEN** the engine SHALL fall back to CPU inference

### Requirement: Audio buffer transcription
The application SHALL accept a 16kHz mono PCM float32 audio buffer (converted from the AudioRecorder's Int16 output) and return the transcribed text string.

#### Scenario: Successful transcription
- **WHEN** a valid audio buffer is provided
- **THEN** the engine SHALL return the transcribed text

#### Scenario: Empty or silent audio
- **WHEN** the audio buffer contains only silence
- **THEN** the engine SHALL return an empty string or minimal noise text

### Requirement: Graceful disposal
The application SHALL dispose the Whisper.net processor and free native resources when the application exits or the model is switched.

#### Scenario: Application exit
- **WHEN** the application is closing
- **THEN** the Whisper processor and model SHALL be disposed to free memory and GPU resources

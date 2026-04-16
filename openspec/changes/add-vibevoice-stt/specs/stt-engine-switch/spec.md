## ADDED Requirements

### Requirement: ISttEngine interface
The application SHALL define an ISttEngine interface with methods for checking availability, loading a model, and transcribing audio. Both WhisperSttService and VibeVoiceSttService SHALL implement this interface.

#### Scenario: Engine implements interface
- **WHEN** the application initializes an STT engine
- **THEN** the engine SHALL implement ISttEngine with Name, IsAvailable, EnsureModelLoadedAsync, and TranscribeAsync members

### Requirement: VibeVoice subprocess bridge
The application SHALL support VibeVoice-ASR as an STT engine via a persistent Python subprocess running vibevoice_server.py. The subprocess SHALL load the model once at startup and accept WAV file paths via stdin, returning JSON transcription results via stdout.

#### Scenario: VibeVoice engine starts
- **WHEN** VibeVoice is selected and LoadAsync is called
- **THEN** the application SHALL start a Python subprocess running vibevoice_server.py and wait for a "READY" signal on stdout

#### Scenario: VibeVoice transcription
- **WHEN** TranscribeAsync is called with an audio buffer
- **THEN** the application SHALL write the audio to a temporary WAV file, send the path via stdin, and return the text from the JSON response on stdout

#### Scenario: VibeVoice subprocess error
- **WHEN** the Python subprocess exits unexpectedly or returns an error JSON
- **THEN** the application SHALL report the error and allow fallback to Whisper

### Requirement: VibeVoice availability detection
The application SHALL detect whether VibeVoice is available by checking if Python and the vibevoice package are installed and importable.

#### Scenario: VibeVoice available
- **WHEN** Python is found in PATH and `from vibevoice.modular.modeling_vibevoice_asr import VibeVoiceASRForConditionalGeneration` succeeds
- **THEN** IsAvailable SHALL return true

#### Scenario: VibeVoice not available
- **WHEN** Python is not found or the vibevoice package is not installed
- **THEN** IsAvailable SHALL return false

### Requirement: Runtime engine switching
The application SHALL allow switching between Whisper and VibeVoice STT engines at runtime via the tray menu. Switching SHALL dispose the current engine and initialize the new one.

#### Scenario: Switch from Whisper to VibeVoice
- **WHEN** user selects VibeVoice from the STT Engine menu
- **THEN** the Whisper engine SHALL be disposed, VibeVoice SHALL be initialized, and subsequent transcriptions SHALL use VibeVoice

#### Scenario: Switch to unavailable engine
- **WHEN** user attempts to select an engine that is not available
- **THEN** the menu item SHALL be disabled (grayed out) and no switch SHALL occur

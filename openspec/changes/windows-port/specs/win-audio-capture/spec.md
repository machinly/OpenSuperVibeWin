## ADDED Requirements

### Requirement: Microphone capture via WASAPI
The application SHALL capture audio from the default microphone using NAudio's WasapiCapture, accumulating PCM data in memory during a recording session.

#### Scenario: Start recording
- **WHEN** a recording session begins
- **THEN** the application SHALL start capturing audio from the default input device

#### Scenario: Stop recording
- **WHEN** a recording session ends
- **THEN** the application SHALL stop capture and make the accumulated PCM buffer available for transcription

### Requirement: Resample to 16kHz mono PCM Int16
The application SHALL resample captured audio to 16kHz, mono channel, 16-bit signed integer PCM format, regardless of the input device's native format.

#### Scenario: High sample rate input
- **WHEN** the microphone provides 48kHz stereo audio
- **THEN** the output buffer SHALL contain 16kHz mono Int16 PCM data

### Requirement: RMS level for waveform visualization
The application SHALL compute RMS audio level (normalized 0.0–1.0) from the captured buffer in real-time to drive the waveform HUD animation.

#### Scenario: Audio level during speech
- **WHEN** the user is speaking during recording
- **THEN** the RMS level callback SHALL provide values reflecting the audio amplitude

#### Scenario: Silence
- **WHEN** no audio input is detected
- **THEN** the RMS level SHALL report values near 0.0

### Requirement: WAV file export
The application SHALL write the accumulated PCM buffer to a temporary WAV file upon recording stop, for consumption by the STT bridge.

#### Scenario: Recording completed
- **WHEN** recording stops and PCM data is available
- **THEN** a valid WAV file (16kHz mono Int16) SHALL be written to a temporary path

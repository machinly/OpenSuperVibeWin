## ADDED Requirements

### Requirement: Floating transparent overlay window
The application SHALL display a topmost, transparent, borderless window for the HUD overlay. The window SHALL appear during active sessions and hide when idle.

#### Scenario: Session starts
- **WHEN** a recording session begins
- **THEN** the overlay window SHALL appear centered horizontally at the bottom of the primary screen

#### Scenario: Session ends
- **WHEN** the session completes or is cancelled
- **THEN** the overlay window SHALL hide after a brief display of the result

### Requirement: Cyberpunk visual style
The overlay SHALL render with the cyberpunk aesthetic: neon glow borders, scan-line effects, semi-transparent dark background, and animated elements.

#### Scenario: Transcription mode appearance
- **WHEN** the session is in transcription mode
- **THEN** the HUD SHALL use cyan/teal neon color theme

#### Scenario: Translation mode appearance
- **WHEN** the session is in translation mode
- **THEN** the HUD SHALL use magenta/pink neon color theme

### Requirement: Animated waveform bars
The overlay SHALL display animated vertical bars reflecting real-time audio RMS levels during recording, matching the 12-bar waveform design of the macOS version.

#### Scenario: Audio input drives animation
- **WHEN** user is speaking during recording
- **THEN** the waveform bars SHALL animate in response to audio amplitude

### Requirement: Text result display
The overlay SHALL display transcription and translation results in styled text cards with the cyberpunk aesthetic.

#### Scenario: Transcription-only result
- **WHEN** transcription completes without translation
- **THEN** a single text card SHALL display the transcribed text

#### Scenario: Translation result
- **WHEN** translation completes
- **THEN** two text cards SHALL display: original transcription (ASR) and translated text (LLM)

### Requirement: Processing state indicators
The overlay SHALL show visual indicators during processing stages (recognizing, translating) such as spinning arcs or shimmer animations.

#### Scenario: Recognizing state
- **WHEN** the STT engine is processing audio
- **THEN** the overlay SHALL display a loading/processing animation

#### Scenario: Translating state
- **WHEN** the LLM is processing the translation
- **THEN** the overlay SHALL display a translating indicator

### Requirement: Error state display
The overlay SHALL display error information when the session enters an error state, then automatically return to hidden after a timeout.

#### Scenario: STT error
- **WHEN** the STT engine fails to transcribe
- **THEN** the overlay SHALL display an error message and auto-hide after a brief delay

#### Scenario: LLM error with fallback
- **WHEN** the LLM call fails or is refused
- **THEN** the overlay SHALL display the raw ASR text as fallback

### Requirement: Overlay does not steal focus
The overlay window SHALL use WS_EX_NOACTIVATE (or WPF equivalent) to prevent stealing focus from the currently active application. This is critical for the paste-to-active-app workflow.

#### Scenario: Overlay appears while user is typing
- **WHEN** the overlay window appears during a recording session
- **THEN** the previously focused application SHALL remain focused and active

#### Scenario: Click on overlay
- **WHEN** user clicks on the overlay window
- **THEN** the previously focused application SHALL remain the foreground window

## ADDED Requirements

### Requirement: STT Engine submenu
The tray context menu SHALL include an "STT Engine" submenu listing available engines (Whisper, VibeVoice). The currently active engine SHALL be checked. Unavailable engines SHALL be grayed out.

#### Scenario: Both engines available
- **WHEN** both Whisper and VibeVoice are available
- **THEN** both menu items SHALL be enabled, with the active engine checked

#### Scenario: VibeVoice not available
- **WHEN** VibeVoice is not available (Python/vibevoice not installed)
- **THEN** the VibeVoice menu item SHALL be disabled with text indicating it's not installed

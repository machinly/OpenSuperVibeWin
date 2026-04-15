## ADDED Requirements

### Requirement: Low-level keyboard hook for global hotkeys
The application SHALL install a low-level keyboard hook (WH_KEYBOARD_LL via SetWindowsHookEx) to intercept key events system-wide. The hook SHALL distinguish Left Alt (VK_LMENU, 0xA4) from Right Alt (VK_RMENU, 0xA5) using the virtual key code. Only Right Alt events SHALL trigger application actions; Left Alt SHALL be passed through unmodified.

#### Scenario: Right Alt detected, Left Alt ignored
- **WHEN** user presses Left Alt
- **THEN** the key event SHALL be passed through to the system without any application action

#### Scenario: Hook installed on startup
- **WHEN** the application starts
- **THEN** a WH_KEYBOARD_LL hook SHALL be installed on a thread with an active message pump

### Requirement: Right Alt toggles transcription
Pressing and releasing Right Alt (VK_RMENU) SHALL toggle transcription recording. First press starts recording; second press stops recording and begins transcription.

#### Scenario: Start transcription
- **WHEN** user presses Right Alt while idle
- **THEN** a recording session SHALL begin in transcription mode (cyan UI)

#### Scenario: Stop transcription
- **WHEN** user presses Right Alt while recording
- **THEN** recording SHALL stop and the transcription pipeline SHALL begin

### Requirement: Alt+/ toggles translation
When Right Alt is held and / (VK_OEM_2) is pressed, the application SHALL toggle translation mode. The hook SHALL track Right Alt key-down state and detect the / key while Alt is held.

#### Scenario: Start translation
- **WHEN** user presses Right Alt + / while idle
- **THEN** a recording session SHALL begin in translation mode (magenta UI)

#### Scenario: Stop translation
- **WHEN** user presses Right Alt + / while recording in translation mode
- **THEN** recording SHALL stop and the translation pipeline SHALL begin

### Requirement: ESC cancels session
The hook SHALL detect the ESC key (VK_ESCAPE) during active sessions. Pressing ESC SHALL cancel any in-progress recording, transcription, or translation and return to idle state.

#### Scenario: Cancel during recording
- **WHEN** user presses ESC while recording
- **THEN** the recording SHALL be discarded and the application SHALL return to idle

#### Scenario: ESC while idle
- **WHEN** user presses ESC while no session is active
- **THEN** the key event SHALL be passed through without any application action

### Requirement: Hotkeys work system-wide without admin
The low-level keyboard hook SHALL function regardless of which application has foreground focus. The hook SHALL NOT require administrator privileges or UAC elevation.

#### Scenario: Hotkey from another application
- **WHEN** user presses Right Alt while a different application is focused
- **THEN** the recording session SHALL start as normal

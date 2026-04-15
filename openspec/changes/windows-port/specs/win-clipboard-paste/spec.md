## ADDED Requirements

### Requirement: Write text to clipboard
The application SHALL write the final result text to the Windows clipboard using the Win32 Clipboard API.

#### Scenario: Transcription result ready
- **WHEN** the final text (transcribed or translated) is ready
- **THEN** the text SHALL be placed on the Windows clipboard as Unicode text

### Requirement: Simulate Ctrl+V paste into foreground app
The application SHALL simulate a Ctrl+V keystroke using the Win32 SendInput API to paste the clipboard content into the currently focused application. The application MUST ensure the overlay window does not steal focus (see win-overlay-hud no-focus requirement) so that SendInput targets the correct window.

#### Scenario: Paste after transcription
- **WHEN** clipboard has been set with the result text
- **THEN** a Ctrl+V keystroke SHALL be simulated, pasting text into the active application that had focus before the session began

#### Scenario: Paste while overlay is visible
- **WHEN** the overlay HUD is displayed and paste is triggered
- **THEN** the paste SHALL target the user's foreground application, NOT the overlay window

### Requirement: Optional Enter key after paste
The application SHALL optionally simulate an Enter key press after pasting, consistent with the macOS version behavior.

#### Scenario: Enter key after paste
- **WHEN** paste is complete and Enter-after-paste is enabled
- **THEN** an Enter keystroke SHALL be simulated after the paste

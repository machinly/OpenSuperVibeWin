## ADDED Requirements

### Requirement: System tray icon presence
The application SHALL display a persistent icon in the Windows system tray (notification area) when running. The icon SHALL use a waveform-style design consistent with the macOS version.

#### Scenario: Application startup
- **WHEN** the application starts
- **THEN** a waveform icon appears in the system tray notification area

#### Scenario: Icon state reflects recording
- **WHEN** a recording session is active
- **THEN** the tray icon SHALL visually change to indicate recording state (e.g., color or animation change)

### Requirement: Context menu with core actions
The application SHALL show a right-click context menu from the tray icon with: Start/Stop Recording, Translation Language submenu (Off, English, Chinese, Japanese, Korean, French, Spanish, German), STT Model selection, LLM Provider settings, Polish toggle, and Quit.

#### Scenario: Right-click tray icon
- **WHEN** user right-clicks the system tray icon
- **THEN** the context menu SHALL appear with all configured menu items

#### Scenario: Select translation language
- **WHEN** user selects a language from the Translation Language submenu
- **THEN** the selected language SHALL be persisted to config and used for subsequent translation sessions

### Requirement: LLM API key input
The application SHALL provide a dialog accessible from the tray menu for entering Anthropic and Gemini API keys, and selecting LLM provider and model.

#### Scenario: Enter API key
- **WHEN** user opens the API key dialog and enters a key
- **THEN** the key SHALL be saved to config and used for subsequent LLM calls

### Requirement: Minimize to tray
The application SHALL have no visible main window. It SHALL run entirely from the system tray, matching the macOS menu-bar-only behavior.

#### Scenario: Application launch
- **WHEN** the application launches
- **THEN** no main window SHALL appear; only the tray icon SHALL be visible

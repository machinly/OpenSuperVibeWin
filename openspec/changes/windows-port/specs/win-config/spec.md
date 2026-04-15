## ADDED Requirements

### Requirement: Config file location
The application SHALL store its configuration at `%APPDATA%\SuperVibe\config.json`. The directory SHALL be created automatically if it does not exist.

#### Scenario: First launch
- **WHEN** the application starts for the first time
- **THEN** the `%APPDATA%\SuperVibe\` directory SHALL be created and a default config SHALL be written

#### Scenario: Existing config
- **WHEN** the application starts and config.json exists
- **THEN** the config SHALL be loaded and applied to all services

### Requirement: Persisted settings
The config SHALL persist: `translation_language`, `stt_model`, `llm_api_key` (Anthropic), `gemini_api_key`, `llm_provider` ("claude" or "gemini"), `polish_enabled`, `llm_model`, `gemini_model`.

#### Scenario: Setting changed
- **WHEN** the user changes any setting via the tray menu
- **THEN** the config file SHALL be updated immediately

#### Scenario: Config roundtrip
- **WHEN** the application saves and reloads config
- **THEN** all persisted values SHALL be preserved exactly

### Requirement: Config format compatibility
The config JSON SHALL use the same key names as the macOS version for shared fields (`translation_language`, `llm_api_key`, `gemini_api_key`, `llm_provider`, `polish_enabled`, `llm_model`, `gemini_model`). Platform-specific fields (`stt_model`) MAY differ in value format (macOS uses mlx-audio model IDs, Windows uses Whisper GGML model sizes like "small", "medium", "large"). Unknown fields SHALL be preserved on read/write to support cross-platform config sharing.

#### Scenario: macOS config loaded on Windows
- **WHEN** a config.json from the macOS version is placed in the Windows config directory
- **THEN** the application SHALL read all shared fields correctly, use default values for platform-specific fields with incompatible values, and preserve unknown fields

#### Scenario: Unknown fields preserved
- **WHEN** the config contains fields not recognized by the Windows version (e.g., `vibevoice_model`)
- **THEN** the application SHALL preserve those fields when writing config back to disk

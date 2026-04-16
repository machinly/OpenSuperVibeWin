## ADDED Requirements

### Requirement: STT engine configuration
The config SHALL persist a `stt_engine` field with values "whisper" or "vibevoice". The default value SHALL be "whisper". When the field is missing from config.json, the application SHALL use "whisper".

#### Scenario: Config with stt_engine
- **WHEN** config.json contains `"stt_engine": "vibevoice"`
- **THEN** the application SHALL initialize with the VibeVoice STT engine (if available, otherwise fall back to Whisper)

#### Scenario: Config without stt_engine
- **WHEN** config.json does not contain the stt_engine field
- **THEN** the application SHALL default to the Whisper engine

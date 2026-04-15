## ADDED Requirements

### Requirement: Claude API integration
The application SHALL support calling Anthropic's Claude API for text polishing and translation, using HttpClient with async/await. It SHALL support model selection from: Haiku 4.5, Sonnet 4, Sonnet 4.5, Sonnet 4.6.

#### Scenario: Polish transcription with Claude
- **WHEN** polish is enabled and Claude is the selected provider
- **THEN** the transcribed text SHALL be sent to the Claude API for grammar/punctuation correction

#### Scenario: Translate with Claude
- **WHEN** translation mode is active and Claude is the selected provider
- **THEN** the transcribed text SHALL be sent to Claude with a translation prompt for the target language

### Requirement: Gemini API integration
The application SHALL support calling Google's Gemini API for text polishing and translation. It SHALL support model selection from: Gemini 3 Flash, 3.1 Flash Lite, 3.1 Pro, 2.5 Flash Lite, 2.5 Flash, 2.5 Pro, 2.0 Flash.

#### Scenario: Polish transcription with Gemini
- **WHEN** polish is enabled and Gemini is the selected provider
- **THEN** the transcribed text SHALL be sent to the Gemini API for grammar/punctuation correction

#### Scenario: Translate with Gemini
- **WHEN** translation mode is active and Gemini is the selected provider
- **THEN** the transcribed text SHALL be sent to Gemini with a translation prompt

### Requirement: Refusal detection
The application SHALL detect LLM refusal responses (safety filters, content policy blocks) and fall back to the raw ASR text when a refusal is detected.

#### Scenario: LLM refuses to process
- **WHEN** the LLM response contains refusal markers (e.g., "I cannot", "I apologize")
- **THEN** the application SHALL discard the LLM response and use the raw transcription text

### Requirement: Provider and model switching
The application SHALL allow switching between Claude and Gemini providers, and between available models within each provider, at runtime via the tray menu.

#### Scenario: Switch provider
- **WHEN** user selects a different LLM provider from the menu
- **THEN** subsequent LLM calls SHALL use the newly selected provider and its configured model

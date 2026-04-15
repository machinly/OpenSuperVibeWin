namespace SuperVibe.Models;

public record LlmModelDef(string Name, string Id);

public static class LlmModels
{
    public static readonly LlmModelDef[] ClaudeModels =
    [
        new("Claude Haiku 4.5", "claude-haiku-4-5-20251001"),
        new("Claude Sonnet 4", "claude-sonnet-4-20250514"),
        new("Claude Sonnet 4.5", "claude-sonnet-4-5-20250929"),
        new("Claude Sonnet 4.6", "claude-sonnet-4-6"),
    ];

    public static readonly LlmModelDef[] GeminiModels =
    [
        new("Gemini 3 Flash", "gemini-3-flash-preview"),
        new("Gemini 3.1 Flash Lite", "gemini-3.1-flash-lite-preview"),
        new("Gemini 3.1 Pro", "gemini-3.1-pro-preview"),
        new("Gemini 2.5 Flash Lite", "gemini-2.5-flash-lite"),
        new("Gemini 2.5 Flash", "gemini-2.5-flash"),
        new("Gemini 2.5 Pro", "gemini-2.5-pro"),
        new("Gemini 2.0 Flash", "gemini-2.0-flash"),
    ];
}

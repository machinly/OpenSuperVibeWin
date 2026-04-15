using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SuperVibe.Models;

public class AppConfig
{
    [JsonPropertyName("translation_language")]
    public string? TranslationLanguage { get; set; }

    [JsonPropertyName("stt_model")]
    public string SttModel { get; set; } = "small";

    [JsonPropertyName("llm_api_key")]
    public string? LlmApiKey { get; set; }

    [JsonPropertyName("gemini_api_key")]
    public string? GeminiApiKey { get; set; }

    [JsonPropertyName("llm_provider")]
    public string LlmProvider { get; set; } = "claude";

    [JsonPropertyName("polish_enabled")]
    public bool PolishEnabled { get; set; } = true;

    [JsonPropertyName("llm_model")]
    public string? LlmModel { get; set; }

    [JsonPropertyName("gemini_model")]
    public string? GeminiModel { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

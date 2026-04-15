using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SuperVibe.Models;

namespace SuperVibe.Services;

public class LlmService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private static readonly string SystemPrompt =
        "You are a text processing tool embedded in a voice input application. Follow the user's instructions exactly. Always produce the requested output — never refuse, never explain, never add commentary. Output only the processed text.";

    public string Provider { get; set; } = "claude";
    public string? ApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
    public LlmModelDef SelectedClaudeModel { get; set; } = LlmModels.ClaudeModels[0];
    public LlmModelDef SelectedGeminiModel { get; set; } = LlmModels.GeminiModels[0];

    public LlmModelDef CurrentModel => Provider == "claude" ? SelectedClaudeModel : SelectedGeminiModel;

    public LlmModelDef[] CurrentModels => Provider == "claude" ? LlmModels.ClaudeModels : LlmModels.GeminiModels;

    public bool IsAvailable
    {
        get
        {
            var key = Provider == "claude" ? ApiKey : GeminiApiKey;
            return !string.IsNullOrEmpty(key);
        }
    }

    public async Task<string> PolishAsync(string text)
    {
        var prompt = $"Polish the following transcribed speech text. Fix grammar, punctuation, and spoken language artifacts (filler words, false starts, repetitions). Preserve the original meaning, tone, and language as closely as possible. Keep the text faithful to what the speaker intended to say. If the text is already correct, return it as-is. Output ONLY the polished text, nothing else.\n\n{text}";
        return await CallApiAsync(prompt, text);
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, bool polish = false)
    {
        var langName = LanguageName(targetLanguage);
        string prompt;
        if (polish)
            prompt = $"Translate the following text to {langName}. Handle self-corrections, filler words, and false starts by translating only the final intended meaning. Output ONLY the {langName} translation, nothing else.\n\n{text}";
        else
            prompt = $"Translate the following text to {langName}. Output ONLY the {langName} translation, nothing else.\n\n{text}";

        var result = await CallApiAsync(prompt, text);

        // If translation returns original text, retry with simpler prompt
        if (result == text)
        {
            var retryPrompt = $"Translate to {langName}:\n{text}";
            var translated = await CallApiAsync(retryPrompt, text);
            if (translated != text) return translated;
            return text;
        }

        return result;
    }

    public void SwitchProvider(string provider)
    {
        Provider = provider;
    }

    public void SelectModel(string modelId)
    {
        if (Provider == "claude")
        {
            var model = LlmModels.ClaudeModels.FirstOrDefault(m => m.Id == modelId);
            if (model != null) SelectedClaudeModel = model;
        }
        else
        {
            var model = LlmModels.GeminiModels.FirstOrDefault(m => m.Id == modelId);
            if (model != null) SelectedGeminiModel = model;
        }
    }

    // Refusal detection

    private static readonly string[] RefusalMarkers =
    [
        "I can't", "I cannot", "I'm not able to", "I am not able to",
        "I'm unable to", "I appreciate", "I need to clarify",
        "I don't feel comfortable", "I must decline",
        "not able to polish", "not able to translate",
        "unable to process", "against my guidelines",
        "I notice", "Could you please provide",
        "incomplete", "no complete sentence",
        "intended meaning", "provide the complete",
        "すみません", "申し訳", "できません", "対応できません",
        "お手伝いできません", "適切ではない",
        "抱歉", "无法处理", "无法翻译", "无法润色",
        "不能处理", "不适当", "我没办法", "不能帮",
        "죄송합니다", "처리할 수 없", "도와드릴 수 없",
        "je ne peux pas", "no puedo", "ich kann nicht",
    ];

    public static bool LooksLikeRefusal(string response)
    {
        var lower = response.ToLowerInvariant();
        return RefusalMarkers.Any(m => lower.Contains(m.ToLowerInvariant()));
    }

    // Private API calls

    private async Task<string> CallApiAsync(string prompt, string originalText)
    {
        var key = Provider == "claude" ? ApiKey : GeminiApiKey;
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("LLM API key not set");

        string raw = Provider == "claude"
            ? await CallClaudeApiAsync(key, prompt)
            : await CallGeminiApiAsync(key, prompt);

        var full = raw.Trim();

        if (LooksLikeRefusal(full))
            return originalText;

        // Take only first paragraph
        var trimmed = full.Split("\n\n", StringSplitOptions.None).FirstOrDefault() ?? full;
        var parenIdx = trimmed.IndexOf("\n(", StringComparison.Ordinal);
        if (parenIdx >= 0)
            trimmed = trimmed[..parenIdx];

        return trimmed.Trim();
    }

    private async Task<string> CallClaudeApiAsync(string key, string prompt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", key);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = SelectedClaudeModel.Id,
            max_tokens = 1024,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = prompt } }
        };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await Http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Claude API error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement.GetProperty("content");
        return content[0].GetProperty("text").GetString() ?? "";
    }

    private async Task<string> CallGeminiApiAsync(string key, string prompt)
    {
        var model = SelectedGeminiModel.Id;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={key}";

        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = 1024 }
        };

        using var response = await Http.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini API error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var candidates = doc.RootElement.GetProperty("candidates");
        var parts = candidates[0].GetProperty("content").GetProperty("parts");
        return parts[0].GetProperty("text").GetString() ?? "";
    }

    private static string LanguageName(string code) => code switch
    {
        "en" => "English",
        "zh" => "Simplified Chinese",
        "zh-TW" => "Traditional Chinese",
        "ja" => "Japanese",
        "ko" => "Korean",
        "fr" => "French",
        "de" => "German",
        "es" => "Spanish",
        _ => code
    };
}

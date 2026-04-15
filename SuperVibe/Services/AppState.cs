using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using SuperVibe.Models;

namespace SuperVibe.Services;

public class AppState : INotifyPropertyChanged, IDisposable
{
    private readonly ConfigService _configService = new();
    private AppConfig _config;

    // Services
    private readonly AudioRecorder _audioRecorder = new();
    private readonly WhisperSttService _whisperStt = new();
    private readonly LlmService _llmService = new();
    private readonly HotkeyManager _hotkeyManager = new();

    // State
    private SessionStage _stage = SessionStage.Idle;
    private string _statusText = "Ready";
    private string _asrText = "";
    private string _gptText = "";
    private bool _isRecording;
    private bool _sessionTranslate;
    private CancellationTokenSource? _processingCts;
    private Dispatcher? _dispatcher;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<bool>? RecordingChanged;
    public event Action? ConfigChanged;
    public event Action<string, string>? Alert;

    /// <summary>
    /// Called when the overlay should be updated. Args: stage, isTranslation, asrText, gptText, errorText
    /// </summary>
    public event Action<SessionStage, bool, string, string, string>? OverlayUpdate;

    /// <summary>
    /// Called when the overlay should be hidden.
    /// </summary>
    public event Action? OverlayHide;

    /// <summary>
    /// Called with audio RMS level for waveform.
    /// </summary>
    public event Action<float>? AudioLevelUpdate;

    public AppState()
    {
        _config = _configService.Load();
        SyncConfigToLlmService();
    }

    public void Initialize(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        // Wire up hotkeys — use BeginInvoke (not Invoke) to avoid blocking the
        // low-level keyboard hook callback, which has a strict timeout (~300ms).
        // Blocking via Invoke risks Windows silently removing the hook.
        _hotkeyManager.OnToggleRecord = () => dispatcher.BeginInvoke(ToggleRecording);
        _hotkeyManager.OnToggleTranslate = () => dispatcher.BeginInvoke(ToggleTranslateRecording);
        _hotkeyManager.OnCancel = () => dispatcher.BeginInvoke(Cancel);
        _hotkeyManager.Start();

        // Wire up audio level callback
        _audioRecorder.OnAudioLevel = level => AudioLevelUpdate?.Invoke(level);

        Debug.WriteLine("[AppState] Initialized");
    }

    // Properties

    public SessionStage Stage
    {
        get => _stage;
        private set { _stage = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsIdle)); }
    }

    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    public string AsrText
    {
        get => _asrText;
        private set { _asrText = value; OnPropertyChanged(); }
    }

    public string GptText
    {
        get => _gptText;
        private set { _gptText = value; OnPropertyChanged(); }
    }

    public bool IsRecording
    {
        get => _isRecording;
        private set { _isRecording = value; OnPropertyChanged(); RecordingChanged?.Invoke(value); }
    }

    public bool IsIdle => _stage == SessionStage.Idle;
    public AppConfig Config => _config;

    // Config accessors

    public bool PolishEnabled
    {
        get => _config.PolishEnabled;
        set { _config.PolishEnabled = value; SaveConfig(); OnPropertyChanged(); }
    }

    public string? TranslationLanguage
    {
        get => _config.TranslationLanguage;
        set { _config.TranslationLanguage = value; SaveConfig(); OnPropertyChanged(); }
    }

    public string LlmProvider
    {
        get => _config.LlmProvider;
        set
        {
            _config.LlmProvider = value;
            _llmService.Provider = value;
            SaveConfig(); OnPropertyChanged(); ConfigChanged?.Invoke();
        }
    }

    public string? LlmApiKey
    {
        get => _config.LlmApiKey;
        set
        {
            _config.LlmApiKey = string.IsNullOrEmpty(value) ? null : value;
            _llmService.ApiKey = _config.LlmApiKey;
            SaveConfig();
        }
    }

    public string? GeminiApiKey
    {
        get => _config.GeminiApiKey;
        set
        {
            _config.GeminiApiKey = string.IsNullOrEmpty(value) ? null : value;
            _llmService.GeminiApiKey = _config.GeminiApiKey;
            SaveConfig();
        }
    }

    public string SttModel
    {
        get => _config.SttModel;
        set { _config.SttModel = value; SaveConfig(); OnPropertyChanged(); ConfigChanged?.Invoke(); }
    }

    public string? LlmModel
    {
        get => _config.LlmModel;
        set
        {
            _config.LlmModel = value;
            if (value != null) _llmService.SelectModel(value);
            SaveConfig(); OnPropertyChanged(); ConfigChanged?.Invoke();
        }
    }

    public string? GeminiModel
    {
        get => _config.GeminiModel;
        set
        {
            _config.GeminiModel = value;
            if (value != null) _llmService.SelectModel(value);
            SaveConfig(); OnPropertyChanged(); ConfigChanged?.Invoke();
        }
    }

    // Session control — full pipeline

    public void ToggleRecording()
    {
        Debug.WriteLine($"[AppState] ToggleRecording -- isRecording={IsRecording}");
        if (IsRecording) StopSession(); else StartSession(false);
    }

    public void ToggleTranslateRecording()
    {
        Debug.WriteLine($"[AppState] ToggleTranslateRecording -- isRecording={IsRecording}");
        if (IsRecording)
        {
            StopSession();
        }
        else if (TranslationLanguage != null)
        {
            StartSession(true);
        }
        else
        {
            Debug.WriteLine("[AppState] No translation language set");
            Alert?.Invoke("Translation Not Configured", "Please select a target language in the Translation menu.");
        }
    }

    public void StartSession(bool translate)
    {
        if (Stage != SessionStage.Idle) return;

        _sessionTranslate = translate;
        AsrText = "";
        GptText = "";
        Stage = SessionStage.Recording;
        IsRecording = true;
        StatusText = "Recording...";
        Debug.WriteLine($"[AppState] Recording started -- mode={( translate ? $"translate({TranslationLanguage})" : "transcribe")}");

        OverlayUpdate?.Invoke(SessionStage.Recording, _sessionTranslate, "", "", "");

        try
        {
            _audioRecorder.Start();
        }
        catch (Exception ex)
        {
            Stage = SessionStage.Error;
            IsRecording = false;
            StatusText = $"Mic error: {ex.Message}";
            OverlayUpdate?.Invoke(SessionStage.Error, _sessionTranslate, "", "", ex.Message);
            ScheduleReset(3.0);
            Debug.WriteLine($"[AppState] Mic error: {ex}");
        }
    }

    public void StopSession()
    {
        if (!IsRecording) return;
        IsRecording = false;
        _audioRecorder.Stop();

        if (Stage == SessionStage.Recording)
        {
            Stage = SessionStage.Recognizing;
            StatusText = "Transcribing...";
            OverlayUpdate?.Invoke(SessionStage.Recognizing, _sessionTranslate, "", "", "");
        }

        Debug.WriteLine("[AppState] Recording stopped, starting transcription pipeline");
        _processingCts?.Cancel();
        _processingCts = new CancellationTokenSource();
        _ = RunTranscriptionPipeline(_processingCts.Token);
    }

    public void Cancel()
    {
        if (Stage == SessionStage.Idle) return;
        Debug.WriteLine("[AppState] Session cancelled (ESC)");

        _processingCts?.Cancel();
        _processingCts = null;

        if (IsRecording)
        {
            IsRecording = false;
            _audioRecorder.Stop();
        }

        Stage = SessionStage.Idle;
        StatusText = "Ready";
        OverlayHide?.Invoke();
    }

    // Pipeline

    private async Task RunTranscriptionPipeline(CancellationToken ct)
    {
        try
        {
            // Get audio buffer
            var audioBuffer = _audioRecorder.GetBufferAsFloat32();
            if (audioBuffer.Length == 0)
            {
                Debug.WriteLine("[AppState] No audio data");
                RunOnUI(() => ScheduleReset(1.0));
                return;
            }

            // Load Whisper model if needed
            await _whisperStt.LoadModelAsync(_config.SttModel, ct);
            if (ct.IsCancellationRequested) return;

            // Transcribe
            var sw = Stopwatch.StartNew();
            var text = await _whisperStt.TranscribeAsync(audioBuffer, ct);
            Debug.WriteLine($"[Whisper] ASR {sw.Elapsed.TotalSeconds:F1}s -- {text[..Math.Min(80, text.Length)]}");
            if (ct.IsCancellationRequested) return;

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.WriteLine("[Whisper] No speech detected");
                RunOnUI(() => ScheduleReset(1.0));
                return;
            }

            RunOnUI(() =>
            {
                AsrText = text;
                OverlayUpdate?.Invoke(SessionStage.Recognizing, _sessionTranslate, text, "", "");
            });

            // Determine if LLM post-processing is needed
            var needsTranslation = _sessionTranslate && TranslationLanguage != null;
            var needsPolish = !_sessionTranslate && PolishEnabled;
            var needsLlm = (needsTranslation || needsPolish) && _llmService.IsAvailable;

            if (needsLlm)
            {
                RunOnUI(() =>
                {
                    Stage = SessionStage.Translating;
                    StatusText = needsPolish ? "Polishing..." : "Translating...";
                    OverlayUpdate?.Invoke(SessionStage.Translating, _sessionTranslate, text, "", "");
                });

                if (ct.IsCancellationRequested) return;

                sw.Restart();
                string result;
                if (needsTranslation && TranslationLanguage != null)
                    result = await _llmService.TranslateAsync(text, TranslationLanguage, PolishEnabled);
                else
                    result = await _llmService.PolishAsync(text);

                Debug.WriteLine($"[LLM] {sw.Elapsed.TotalSeconds:F1}s -- {result[..Math.Min(80, result.Length)]}");
                if (ct.IsCancellationRequested) return;

                RunOnUI(() =>
                {
                    GptText = result;
                    OverlayUpdate?.Invoke(SessionStage.Translating, _sessionTranslate, text, result, "");
                });
            }

            if (ct.IsCancellationRequested) return;
            RunOnUI(FinishSession);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // User cancelled — Cancel() already handled cleanup
        }
        catch (OperationCanceledException ex)
        {
            // HTTP timeout or other non-user cancellation
            RunOnUI(() =>
            {
                Debug.WriteLine($"[AppState] Pipeline timeout: {ex.Message}");
                Stage = SessionStage.Error;
                StatusText = "Error: Request timed out";
                OverlayUpdate?.Invoke(SessionStage.Error, _sessionTranslate, "", "", "Request timed out");
                ScheduleReset(3.0);
            });
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested || Stage == SessionStage.Idle) return;
            RunOnUI(() =>
            {
                Debug.WriteLine($"[AppState] Pipeline error: {ex}");
                Stage = SessionStage.Error;
                StatusText = $"Error: {ex.Message}";
                OverlayUpdate?.Invoke(SessionStage.Error, _sessionTranslate, "", "", ex.Message);
                ScheduleReset(3.0);
            });
        }
    }

    private void FinishSession()
    {
        if (Stage == SessionStage.Idle) return;
        Stage = SessionStage.Done;

        // Refusal detection
        if (!string.IsNullOrEmpty(GptText) && LlmService.LooksLikeRefusal(GptText))
        {
            Debug.WriteLine("[AppState] GPT refusal detected, falling back to ASR text");
            GptText = "";
        }

        var raw = string.IsNullOrEmpty(GptText) ? AsrText : GptText;
        var finalText = TrimTrailingPunctuation(raw);
        StatusText = "Done";
        OverlayUpdate?.Invoke(SessionStage.Done, _sessionTranslate, AsrText, GptText, "");
        Debug.WriteLine($"[AppState] Finish -- text=\"{finalText[..Math.Min(80, finalText.Length)]}\"");

        if (string.IsNullOrEmpty(finalText))
        {
            ScheduleReset(1.0);
            return;
        }

        // Paste after short delay
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                ClipboardPasteService.Paste(finalText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppState] Paste failed: {ex.Message}");
            }
        };
        timer.Start();

        ScheduleReset(1.5);
    }

    private static string TrimTrailingPunctuation(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var s = text;
        char[] midPunct = ['.', '\u3002', '!', '\uFF01', '?', '\uFF1F'];
        var body = s[..^1];
        var hasMidPunct = body.Any(c => midPunct.Contains(c));
        if (!hasMidPunct && s.Length > 0)
        {
            var last = s[^1];
            if (last == '.' || last == '\u3002')
                s = s[..^1];
        }
        return s;
    }

    private void ScheduleReset(double delaySec)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delaySec) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            OverlayHide?.Invoke();
            Stage = SessionStage.Idle;
            StatusText = "Ready";
        };
        timer.Start();
    }

    private void RunOnUI(Action action)
    {
        if (_dispatcher != null)
            _dispatcher.Invoke(action);
        else
            action();
    }

    private void SyncConfigToLlmService()
    {
        _llmService.Provider = _config.LlmProvider;
        _llmService.ApiKey = _config.LlmApiKey;
        _llmService.GeminiApiKey = _config.GeminiApiKey;

        if (_config.LlmModel != null)
        {
            var model = LlmModels.ClaudeModels.FirstOrDefault(m => m.Id == _config.LlmModel);
            if (model != null) _llmService.SelectedClaudeModel = model;
        }
        if (_config.GeminiModel != null)
        {
            var model = LlmModels.GeminiModels.FirstOrDefault(m => m.Id == _config.GeminiModel);
            if (model != null) _llmService.SelectedGeminiModel = model;
        }
    }

    private void SaveConfig()
    {
        _configService.Save(_config);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Dispose()
    {
        _processingCts?.Cancel();
        _processingCts = null;
        _hotkeyManager.Dispose();
        _audioRecorder.Dispose();
        _whisperStt.Dispose();
    }
}

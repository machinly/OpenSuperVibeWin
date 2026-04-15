using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using SuperVibe.Models;
using SuperVibe.Services;
using WinForms = System.Windows.Forms;

namespace SuperVibe;

public partial class App : Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private readonly AppState _appState = new();

    private static readonly (string Label, string? Code)[] Languages =
    [
        ("Off", null),
        ("English", "en"),
        ("Chinese (Simplified)", "zh"),
        ("Chinese (Traditional)", "zh-TW"),
        ("Japanese", "ja"),
        ("Korean", "ko"),
        ("French", "fr"),
        ("Spanish", "es"),
        ("German", "de"),
    ];

    private static readonly string[] SttModelSizes = ["tiny", "base", "small", "medium", "large"];

    private Views.OverlayWindow? _overlay;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupTrayIcon();
        SetupOverlay();

        _appState.RecordingChanged += OnRecordingChanged;
        _appState.ConfigChanged += RebuildMenu;
        _appState.Initialize(Dispatcher);
    }

    private void SetupOverlay()
    {
        _overlay = new Views.OverlayWindow();

        _appState.OverlayUpdate += (stage, isTrans, asr, gpt, err) =>
        {
            Dispatcher.Invoke(() => _overlay.ShowStage(stage, isTrans, asr, gpt, err));
        };

        _appState.OverlayHide += () =>
        {
            Dispatcher.Invoke(() => _overlay.HideOverlay());
        };

        _appState.AudioLevelUpdate += level =>
        {
            _overlay.PushAudioLevel(level);
        };
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "SuperVibe",
            Visible = true,
        };

        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _trayIcon?.ContextMenuStrip?.Dispose();
        var menu = new WinForms.ContextMenuStrip();

        // Record toggle
        if (_appState.IsRecording)
        {
            menu.Items.Add("Stop Recording", null, (_, _) => _appState.StopSession());
            menu.Items.Add("Cancel (ESC)", null, (_, _) => _appState.Cancel());
        }
        else
        {
            menu.Items.Add("Transcribe (F9)", null, (_, _) => _appState.StartSession(false));
            var translateItem = menu.Items.Add("Translate (Shift+F9)", null, (_, _) => _appState.StartSession(true));
            translateItem.Enabled = _appState.TranslationLanguage != null;
        }

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // Translation language submenu
        var transMenu = new WinForms.ToolStripMenuItem(
            _appState.TranslationLanguage != null
                ? $"Translation ({_appState.TranslationLanguage.ToUpper()})"
                : "Translation");
        foreach (var (label, code) in Languages)
        {
            var langCode = code;
            var item = new WinForms.ToolStripMenuItem(label, null, (_, _) =>
            {
                _appState.TranslationLanguage = langCode;
                Debug.WriteLine($"[SuperVibe] Translation: {_appState.TranslationLanguage ?? "off"}");
                RebuildMenu();
            });
            item.Checked = _appState.TranslationLanguage == code;
            transMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(transMenu);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // STT Model submenu
        var sttMenu = new WinForms.ToolStripMenuItem($"STT Model ({_appState.SttModel})");
        foreach (var size in SttModelSizes)
        {
            var s = size;
            var item = new WinForms.ToolStripMenuItem(size, null, (_, _) =>
            {
                _appState.SttModel = s;
                Debug.WriteLine($"[SuperVibe] STT model: {s}");
                RebuildMenu();
            });
            item.Checked = _appState.SttModel == size;
            sttMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(sttMenu);

        // Polish toggle
        var polishItem = new WinForms.ToolStripMenuItem("Polish", null, (_, _) =>
        {
            _appState.PolishEnabled = !_appState.PolishEnabled;
            Debug.WriteLine($"[SuperVibe] Polish: {(_appState.PolishEnabled ? "ON" : "OFF")}");
            RebuildMenu();
        });
        polishItem.Checked = _appState.PolishEnabled;
        menu.Items.Add(polishItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // LLM Provider submenu
        var providerLabel = _appState.LlmProvider == "claude" ? "Claude" : "Gemini";
        var providerMenu = new WinForms.ToolStripMenuItem($"LLM Provider ({providerLabel})");
        var claudeItem = new WinForms.ToolStripMenuItem("Claude (Anthropic)", null, (_, _) =>
        {
            _appState.LlmProvider = "claude";
            RebuildMenu();
        });
        claudeItem.Checked = _appState.LlmProvider == "claude";
        providerMenu.DropDownItems.Add(claudeItem);

        var geminiItem = new WinForms.ToolStripMenuItem("Gemini (Google)", null, (_, _) =>
        {
            _appState.LlmProvider = "gemini";
            RebuildMenu();
        });
        geminiItem.Checked = _appState.LlmProvider == "gemini";
        providerMenu.DropDownItems.Add(geminiItem);
        menu.Items.Add(providerMenu);

        // LLM Model submenu
        var currentModels = _appState.LlmProvider == "claude" ? LlmModels.ClaudeModels : LlmModels.GeminiModels;
        var currentModelId = _appState.LlmProvider == "claude" ? _appState.LlmModel : _appState.GeminiModel;
        var currentModelName = currentModels.FirstOrDefault(m => m.Id == currentModelId)?.Name ?? currentModels[0].Name;
        var llmModelMenu = new WinForms.ToolStripMenuItem($"LLM Model ({currentModelName})");
        foreach (var model in currentModels)
        {
            var m = model;
            var modelItem = new WinForms.ToolStripMenuItem(model.Name, null, (_, _) =>
            {
                if (_appState.LlmProvider == "claude")
                    _appState.LlmModel = m.Id;
                else
                    _appState.GeminiModel = m.Id;
                Debug.WriteLine($"[SuperVibe] LLM model: {m.Name}");
                RebuildMenu();
            });
            modelItem.Checked = model.Id == currentModelId;
            llmModelMenu.DropDownItems.Add(modelItem);
        }
        menu.Items.Add(llmModelMenu);

        // API Key
        var keyTitle = !string.IsNullOrEmpty(_appState.LlmProvider == "claude" ? _appState.LlmApiKey : _appState.GeminiApiKey)
            ? "LLM API Key (set)"
            : "Set LLM API Key...";
        menu.Items.Add(keyTitle, null, (_, _) => ShowApiKeyDialog());

        menu.Items.Add(new WinForms.ToolStripSeparator());

        // Quit
        menu.Items.Add("Quit SuperVibe", null, (_, _) =>
        {
            _trayIcon!.Visible = false;
            Shutdown();
        });

        _trayIcon!.ContextMenuStrip = menu;
    }

    private void OnRecordingChanged(bool isRecording)
    {
        Dispatcher.Invoke(() =>
        {
            _trayIcon!.Icon = LoadAppIcon(isRecording);
            RebuildMenu();
        });
    }

    private void ShowApiKeyDialog()
    {
        var dialog = new Views.ApiKeyDialog(_appState);
        dialog.ShowDialog();
        RebuildMenu();
    }

    private static Icon LoadAppIcon(bool recording = false)
    {
        try
        {
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (System.IO.File.Exists(iconPath))
                return new Icon(iconPath);
        }
        catch { }
        return SystemIcons.Application;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appState.Dispose();
        _overlay?.Close();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}

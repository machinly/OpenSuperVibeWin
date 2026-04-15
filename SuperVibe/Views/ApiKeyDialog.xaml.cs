using System.Windows;
using SuperVibe.Services;

namespace SuperVibe.Views;

public partial class ApiKeyDialog : Window
{
    private readonly AppState _appState;

    public ApiKeyDialog(AppState appState)
    {
        InitializeComponent();
        _appState = appState;

        // Initialize UI from current state
        ClaudeRadio.IsChecked = _appState.LlmProvider == "claude";
        GeminiRadio.IsChecked = _appState.LlmProvider == "gemini";
        UpdateKeyField();
    }

    private void OnProviderChanged(object sender, RoutedEventArgs e)
    {
        UpdateKeyField();
    }

    private void UpdateKeyField()
    {
        if (ClaudeRadio?.IsChecked == true)
        {
            KeyLabel.Text = "Anthropic API Key";
            KeyInput.Text = _appState.LlmApiKey ?? "";
        }
        else
        {
            KeyLabel.Text = "Google Gemini API Key";
            KeyInput.Text = _appState.GeminiApiKey ?? "";
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var key = KeyInput.Text.Trim();
        var keyValue = string.IsNullOrEmpty(key) ? null : key;

        if (ClaudeRadio.IsChecked == true)
        {
            _appState.LlmProvider = "claude";
            _appState.LlmApiKey = keyValue;
        }
        else
        {
            _appState.LlmProvider = "gemini";
            _appState.GeminiApiKey = keyValue;
        }

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls;
using SuperVibe.Models;

namespace SuperVibe.Views;

public partial class OverlayWindow : Window
{
    // WS_EX_NOACTIVATE: prevent stealing focus
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Colors
    private static readonly Color NeonCyan = Color.FromRgb(0x00, 0xD4, 0xFF);
    private static readonly Color NeonMagenta = Color.FromRgb(0xFF, 0x00, 0xE6);

    private readonly DispatcherTimer _animTimer;
    private readonly Rectangle[] _bars = new Rectangle[12];
    private readonly double[] _barWeights;

    private float _targetLevel;
    private float _smoothedLevel;
    private SessionStage _currentStage = SessionStage.Idle;
    private bool _isTranslation;
    private int _dotPhase;
    private int _dotCounter;

    public OverlayWindow()
    {
        InitializeComponent();

        // Pre-compute bell curve weights for 12 bars
        _barWeights = new double[12];
        for (int i = 0; i < 12; i++)
        {
            double x = (i - 5.5) / 3.0;
            _barWeights[i] = 0.35 + 0.65 * Math.Exp(-x * x);
        }

        // Create waveform bars
        for (int i = 0; i < 12; i++)
        {
            var bar = new Rectangle
            {
                Width = 5,
                Height = 4,
                RadiusX = 2.5,
                RadiusY = 2.5,
                Fill = new SolidColorBrush(NeonCyan),
            };
            _bars[i] = bar;
            WaveformCanvas.Children.Add(bar);
        }

        // Animation timer at ~30 FPS
        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _animTimer.Tick += OnAnimTick;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set WS_EX_NOACTIVATE to prevent focus stealing
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

        PositionBottomCenter();
    }

    public void PositionBottomCenter()
    {
        var screenW = SystemParameters.PrimaryScreenWidth;
        var screenH = SystemParameters.PrimaryScreenHeight;
        Left = (screenW - Width) / 2;
        Top = screenH - Height - 80; // 80px from bottom
    }

    public void PushAudioLevel(float level)
    {
        _targetLevel = level;
    }

    public void ShowStage(SessionStage stage, bool isTranslation, string asrText = "", string gptText = "", string errorText = "")
    {
        _currentStage = stage;
        _isTranslation = isTranslation;
        var accent = isTranslation ? NeonMagenta : NeonCyan;
        var accentBrush = new SolidColorBrush(accent);

        // Hide all panels
        RecordingPanel.Visibility = Visibility.Collapsed;
        TextPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;

        switch (stage)
        {
            case SessionStage.Recording:
                RecordingPanel.Visibility = Visibility.Visible;
                RecordingPanel.BorderBrush = new SolidColorBrush(Color.FromArgb(77, accent.R, accent.G, accent.B));
                RecordingShadow.Color = accent;
                foreach (var bar in _bars)
                    bar.Fill = accentBrush;
                _animTimer.Start();
                break;

            case SessionStage.Recognizing:
            case SessionStage.Translating:
            case SessionStage.Done:
                _animTimer.Stop();
                TextPanel.Visibility = Visibility.Visible;
                TextPanel.BorderBrush = accentBrush;
                TextShadow.Color = accent;
                AsrLabel.Foreground = new SolidColorBrush(NeonCyan);
                AsrLabel.Text = stage == SessionStage.Recognizing ? "TRANSCRIBING" : "TRANSCRIBED";
                AsrTextBlock.Text = TailText(asrText, 400);
                UpdateDots(NeonCyan);

                if (isTranslation || !string.IsNullOrEmpty(gptText))
                {
                    Divider.Visibility = Visibility.Visible;
                    Divider.Fill = new SolidColorBrush(Color.FromArgb(51, accent.R, accent.G, accent.B));
                    GptSection.Visibility = Visibility.Visible;
                    GptLabel.Foreground = accentBrush;
                    GptLabel.Text = stage == SessionStage.Done
                        ? (isTranslation ? "TRANSLATED" : "POLISHED")
                        : (isTranslation ? "TRANSLATING" : "POLISHING");
                    GptTextBlock.Text = TailText(gptText, 400);
                }
                else
                {
                    Divider.Visibility = Visibility.Collapsed;
                    GptSection.Visibility = Visibility.Collapsed;
                }
                break;

            case SessionStage.Error:
                _animTimer.Stop();
                ErrorPanel.Visibility = Visibility.Visible;
                ErrorText.Text = string.IsNullOrEmpty(errorText) ? "ERROR" : errorText;
                break;

            case SessionStage.Idle:
                _animTimer.Stop();
                break;
        }

        if (stage != SessionStage.Idle)
        {
            Show();
            PositionBottomCenter();
        }
    }

    public void HideOverlay()
    {
        _animTimer.Stop();
        _smoothedLevel = 0;
        _targetLevel = 0;
        Hide();
    }

    public void ResetLevels()
    {
        _smoothedLevel = 0;
        _targetLevel = 0;
    }

    private void OnAnimTick(object? sender, EventArgs e)
    {
        // Smoothing filter (same as macOS)
        if (_targetLevel > _smoothedLevel)
            _smoothedLevel += (_targetLevel - _smoothedLevel) * 0.40f;
        else
            _smoothedLevel += (_targetLevel - _smoothedLevel) * 0.15f;

        // Update waveform bars
        var t = Environment.TickCount64 / 1000.0;
        var barSpacing = 8.5; // 5 width + 3.5 spacing
        for (int i = 0; i < 12; i++)
        {
            var w = _barWeights[i];
            var fi = (double)i;
            var j1 = Math.Sin(t * 13.0 + fi * 2.7) * 0.22;
            var j2 = Math.Sin(t * 7.3 + fi * 4.1) * 0.15;
            var jitter = 1.0 + j1 + j2;
            var boosted = Math.Max(_smoothedLevel, 0.2);
            var h = 4 + boosted * w * jitter * (46 - 4);
            h = Math.Max(4, h);

            _bars[i].Height = h;
            Canvas.SetLeft(_bars[i], i * barSpacing);
            Canvas.SetTop(_bars[i], (50 - h) / 2);
        }

        // Dot animation
        _dotCounter++;
        if (_dotCounter >= 10) // ~every 330ms
        {
            _dotCounter = 0;
            _dotPhase = (_dotPhase + 1) % 4;
            if (_currentStage is SessionStage.Recognizing or SessionStage.Translating)
            {
                var accent = _isTranslation ? NeonMagenta : NeonCyan;
                UpdateDots(accent);
            }
        }
    }

    private void UpdateDots(Color accent)
    {
        var bright = new SolidColorBrush(Color.FromArgb(230, accent.R, accent.G, accent.B));
        var dim = new SolidColorBrush(Color.FromArgb(38, accent.R, accent.G, accent.B));
        SpinDot1.Fill = _dotPhase > 0 ? bright : dim;
        SpinDot2.Fill = _dotPhase > 1 ? bright : dim;
        SpinDot3.Fill = _dotPhase > 2 ? bright : dim;
    }

    private static string TailText(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLen) return text;
        return "…" + text[^maxLen..];
    }
}

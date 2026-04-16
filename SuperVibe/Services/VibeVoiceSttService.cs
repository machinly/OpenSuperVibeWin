using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SuperVibe.Services;

public class VibeVoiceSttService : ISttEngine
{
    public string Name => "VibeVoice";

    private bool? _isAvailable;
    public bool IsAvailable => _isAvailable ?? false;

    private Process? _serverProcess;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private bool _ready;

    /// <summary>
    /// Detect VibeVoice availability asynchronously. Call once at startup.
    /// </summary>
    public async Task DetectAvailabilityAsync()
    {
        _isAvailable = await Task.Run(() =>
        {
            try
            {
                var python = FindPython();
                if (python == null) return false;

                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = python,
                    Arguments = "-c \"from vibevoice.modular.modeling_vibevoice_asr import VibeVoiceASRForConditionalGeneration; print('ok')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(10000);
                return proc.ExitCode == 0 && output.Trim() == "ok";
            }
            catch
            {
                return false;
            }
        });

        Debug.WriteLine($"[VibeVoice] Available: {_isAvailable}");
    }

    public async Task EnsureModelLoadedAsync(CancellationToken ct = default)
    {
        if (_ready && _serverProcess is { HasExited: false })
            return;

        // Kill any leftover process
        StopServer();

        var python = FindPython();
        if (python == null)
            throw new InvalidOperationException("Python not found. Install Python and vibevoice package.");

        var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "vibevoice_server.py");
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("vibevoice_server.py not found", scriptPath);

        var proc = new Process();
        proc.StartInfo = new ProcessStartInfo
        {
            FileName = python,
            Arguments = $"\"{scriptPath}\" microsoft/VibeVoice-ASR",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        // Capture stderr to Debug output
        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.WriteLine($"[VibeVoice-py] {e.Data}");
        };

        proc.Start();
        proc.BeginErrorReadLine();
        _serverProcess = proc;
        _stdin = proc.StandardInput;
        _stdout = proc.StandardOutput;

        Debug.WriteLine("[VibeVoice] Waiting for model to load...");

        // Wait for READY signal
        var readyLine = await Task.Run(() =>
        {
            try { return _stdout.ReadLine(); }
            catch { return null; }
        }, ct);

        if (readyLine?.Trim() == "READY")
        {
            _ready = true;
            Debug.WriteLine("[VibeVoice] Server ready");
        }
        else
        {
            StopServer();
            throw new InvalidOperationException("VibeVoice server failed to start");
        }
    }

    public async Task<string> TranscribeAsync(float[] audioBuffer, CancellationToken ct = default)
    {
        if (!_ready || _serverProcess is null or { HasExited: true } || _stdin == null || _stdout == null)
            throw new InvalidOperationException("VibeVoice server not ready");

        // Write audio to temp WAV file
        var wavPath = AudioUtils.WriteWavToTempFile(audioBuffer);

        try
        {
            // Send path to subprocess
            await _stdin.WriteLineAsync(wavPath);
            await _stdin.FlushAsync();

            // Read JSON response
            var responseLine = await Task.Run(() =>
            {
                try { return _stdout.ReadLine(); }
                catch { return null; }
            }, ct);

            if (responseLine == null)
                throw new InvalidOperationException("No response from VibeVoice server");

            using var doc = JsonDocument.Parse(responseLine);
            var root = doc.RootElement;

            if (root.TryGetProperty("ok", out var okProp) && okProp.GetBoolean())
            {
                return root.GetProperty("text").GetString()?.Trim() ?? "";
            }
            else
            {
                var error = root.TryGetProperty("error", out var errProp)
                    ? errProp.GetString() ?? "unknown error"
                    : "unknown error";
                throw new InvalidOperationException($"VibeVoice: {error}");
            }
        }
        finally
        {
            try { File.Delete(wavPath); } catch { }
        }
    }

    private void StopServer()
    {
        _ready = false;
        try { _stdin?.Close(); } catch { }
        try
        {
            if (_serverProcess is { HasExited: false })
            {
                _serverProcess.Kill();
                _serverProcess.WaitForExit(3000);
            }
        }
        catch { }
        _serverProcess?.Dispose();
        _serverProcess = null;
        _stdin = null;
        _stdout = null;
    }

    private static string? FindPython()
    {
        // Prefer project-local venv first
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // Walk up from bin/Release/net8.0-windows/ to project root
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var venvPython = Path.Combine(projectRoot, "venv", "Scripts", "python.exe");
        if (File.Exists(venvPython))
            return venvPython;

        // Fallback: search PATH
        foreach (var name in new[] { "python", "python3" })
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = name,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                proc.Start();
                proc.WaitForExit(5000);
                if (proc.ExitCode == 0) return name;
            }
            catch { }
        }
        return null;
    }

    public void Dispose()
    {
        StopServer();
    }
}

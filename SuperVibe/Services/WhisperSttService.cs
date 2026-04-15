using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace SuperVibe.Services;

public class WhisperSttService : IDisposable
{
    private static readonly string ModelsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SuperVibe", "models");

    private static readonly Dictionary<string, GgmlType> ModelSizeMap = new()
    {
        ["tiny"] = GgmlType.Tiny,
        ["base"] = GgmlType.Base,
        ["small"] = GgmlType.Small,
        ["medium"] = GgmlType.Medium,
        ["large"] = GgmlType.LargeV3,
    };

    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;
    private string? _loadedModel;
    private string? _language;

    /// <summary>
    /// Called with download progress (0.0 - 1.0) during model download.
    /// </summary>
    public Action<double>? OnDownloadProgress { get; set; }

    /// <summary>
    /// Set the language hint for Whisper (e.g., "zh", "ja", "en").
    /// Set to null for auto-detection.
    /// </summary>
    public string? Language
    {
        get => _language;
        set => _language = value;
    }

    /// <summary>
    /// Get the file path for a given model size.
    /// </summary>
    public static string GetModelPath(string modelSize)
    {
        return Path.Combine(ModelsDir, $"ggml-{modelSize}.bin");
    }

    /// <summary>
    /// Check if a model file exists locally.
    /// </summary>
    public static bool IsModelDownloaded(string modelSize)
    {
        return File.Exists(GetModelPath(modelSize));
    }

    /// <summary>
    /// Download a model from Hugging Face if not already present.
    /// </summary>
    public async Task DownloadModelAsync(string modelSize, CancellationToken ct = default)
    {
        if (IsModelDownloaded(modelSize)) return;

        if (!ModelSizeMap.TryGetValue(modelSize, out var ggmlType))
            throw new ArgumentException($"Unknown model size: {modelSize}");

        Directory.CreateDirectory(ModelsDir);
        var modelPath = GetModelPath(modelSize);
        var tempPath = modelPath + ".tmp";

        try
        {
            using var httpClient = new HttpClient();
            var downloader = new WhisperGgmlDownloader(httpClient);
            using var modelStream = await downloader.GetGgmlModelAsync(ggmlType, cancellationToken: ct);
            using var fileStream = File.Create(tempPath);

            var buffer = new byte[81920];
            long totalRead = 0;
            int read;

            while ((read = await modelStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read, ct);
                totalRead += read;
                // Approximate progress based on known model sizes
                var estimatedSize = GetEstimatedModelSize(modelSize);
                if (estimatedSize > 0)
                    OnDownloadProgress?.Invoke((double)totalRead / estimatedSize);
            }

            fileStream.Close();
            File.Move(tempPath, modelPath, overwrite: true);
            OnDownloadProgress?.Invoke(1.0);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// Load or reload the Whisper processor with the specified model.
    /// </summary>
    public async Task LoadModelAsync(string modelSize, CancellationToken ct = default)
    {
        if (_loadedModel == modelSize && _processor != null)
            return;

        // Dispose existing processor
        DisposeProcessor();

        // Download if needed
        await DownloadModelAsync(modelSize, ct);

        var modelPath = GetModelPath(modelSize);
        _factory = WhisperFactory.FromPath(modelPath);
        var processorBuilder = _factory.CreateBuilder();

        if (!string.IsNullOrEmpty(_language))
            processorBuilder.WithLanguage(_language);
        else
            processorBuilder.WithLanguageDetection();

        _processor = processorBuilder.Build();
        _loadedModel = modelSize;
    }

    /// <summary>
    /// Transcribe a float32 audio buffer (16kHz mono).
    /// </summary>
    public async Task<string> TranscribeAsync(float[] audioBuffer, CancellationToken ct = default)
    {
        if (_processor == null)
            throw new InvalidOperationException("Whisper processor not loaded. Call LoadModelAsync first.");

        // Write float32 array into a MemoryStream as WAV for Whisper.net
        using var memStream = new MemoryStream();
        WriteWavToStream(memStream, audioBuffer, 16000);
        memStream.Position = 0;

        var segments = new List<string>();
        await foreach (var segment in _processor.ProcessAsync(memStream, ct))
        {
            segments.Add(segment.Text.Trim());
        }

        return string.Join(" ", segments).Trim();
    }

    /// <summary>
    /// Switch to a different model. Disposes the current processor.
    /// </summary>
    public async Task SwitchModelAsync(string newModelSize, CancellationToken ct = default)
    {
        DisposeProcessor();
        await LoadModelAsync(newModelSize, ct);
    }

    private void DisposeProcessor()
    {
        _processor?.Dispose();
        _processor = null;
        _factory?.Dispose();
        _factory = null;
        _loadedModel = null;
    }

    private static long GetEstimatedModelSize(string modelSize) => modelSize switch
    {
        "tiny" => 75_000_000,
        "base" => 142_000_000,
        "small" => 466_000_000,
        "medium" => 1_500_000_000,
        "large" => 3_000_000_000,
        _ => 0
    };

    private static void WriteWavToStream(Stream stream, float[] samples, int sampleRate)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        int bitsPerSample = 16;
        int channels = 1;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = samples.Length * blockAlign;

        // RIFF header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // chunk size
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1f, 1f);
            writer.Write((short)(clamped * 32767));
        }
    }

    public void Dispose()
    {
        DisposeProcessor();
    }
}

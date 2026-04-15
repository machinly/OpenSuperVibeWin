using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SuperVibe.Services;

public class AudioRecorder : IDisposable
{
    private WasapiCapture? _capture;
    private WaveFormat? _captureFormat;
    private readonly object _dataLock = new();
    private List<byte> _pcmBuffer = new();
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    /// <summary>
    /// Called with normalized RMS level (0.0 - 1.0) during recording.
    /// </summary>
    public Action<float>? OnAudioLevel { get; set; }

    public void Start()
    {
        if (_isRecording) return;
        _capture = new WasapiCapture();
        _captureFormat = _capture.WaveFormat;
        _pcmBuffer = new List<byte>(16000 * 2 * 600); // pre-allocate ~10 min

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
        _capture.StartRecording();
        _isRecording = true;
    }

    public void Stop()
    {
        if (!_isRecording) return;
        _isRecording = false;
        _capture?.StopRecording();
    }

    /// <summary>
    /// Returns accumulated audio as 16kHz mono float32 array for Whisper.net.
    /// Call after Stop().
    /// </summary>
    public float[] GetBufferAsFloat32()
    {
        byte[] rawBytes;
        lock (_dataLock)
        {
            rawBytes = _pcmBuffer.ToArray();
            _pcmBuffer.Clear();
        }

        if (rawBytes.Length == 0 || _captureFormat == null)
            return Array.Empty<float>();

        // Convert raw capture bytes to 16kHz mono float32 via NAudio resampling pipeline
        using var rawStream = new RawSourceWaveStream(rawBytes, 0, rawBytes.Length, _captureFormat);
        var sampleProvider = rawStream.ToSampleProvider();

        // Convert to mono if stereo
        if (_captureFormat.Channels > 1)
            sampleProvider = sampleProvider.ToMono();

        // Resample to 16kHz
        if (_captureFormat.SampleRate != 16000)
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 16000);

        // Read all resampled float32 samples
        var result = new List<float>();
        var readBuffer = new float[4096];
        int read;
        while ((read = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            if (read == readBuffer.Length)
                result.AddRange(readBuffer);
            else
                result.AddRange(readBuffer.AsSpan(0, read).ToArray());
        }

        return result.ToArray();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;

        // Accumulate raw PCM
        lock (_dataLock)
        {
            var span = new ReadOnlySpan<byte>(e.Buffer, 0, e.BytesRecorded);
            _pcmBuffer.AddRange(span.ToArray());
        }

        // Compute RMS for waveform visualization
        ComputeRmsLevel(e.Buffer, e.BytesRecorded);
    }

    private void ComputeRmsLevel(byte[] buffer, int bytesRecorded)
    {
        if (_captureFormat == null || OnAudioLevel == null) return;

        int bytesPerSample = _captureFormat.BitsPerSample / 8;
        int sampleCount = bytesRecorded / bytesPerSample;
        if (sampleCount == 0) return;

        double sumOfSquares = 0;

        if (_captureFormat.BitsPerSample == 32 && _captureFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            for (int i = 0; i < bytesRecorded - 3; i += bytesPerSample * _captureFormat.Channels)
            {
                float sample = BitConverter.ToSingle(buffer, i);
                sumOfSquares += sample * sample;
            }
            sampleCount = bytesRecorded / (bytesPerSample * _captureFormat.Channels);
        }
        else if (_captureFormat.BitsPerSample == 16)
        {
            for (int i = 0; i < bytesRecorded - 1; i += bytesPerSample * _captureFormat.Channels)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                float normalized = sample / 32768f;
                sumOfSquares += normalized * normalized;
            }
            sampleCount = bytesRecorded / (bytesPerSample * _captureFormat.Channels);
        }

        if (sampleCount == 0) return;

        float rms = (float)Math.Sqrt(sumOfSquares / sampleCount);
        float db = 20f * MathF.Log10(Math.Max(rms, 1e-7f));
        float level = Math.Clamp((db + 50f) / 50f, 0f, 1f); // -50dB..0dB -> 0..1

        OnAudioLevel?.Invoke(level);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        // Cleanup handled by Dispose(); no-op here to avoid double-dispose race.
    }

    public void Dispose()
    {
        Stop();
        if (_capture != null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.Dispose();
            _capture = null;
        }
    }
}

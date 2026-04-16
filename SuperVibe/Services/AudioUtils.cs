using System;
using System.IO;

namespace SuperVibe.Services;

public static class AudioUtils
{
    /// <summary>
    /// Write float32 PCM samples as a 16-bit mono WAV to a stream.
    /// </summary>
    public static void WriteWavToStream(Stream stream, float[] samples, int sampleRate = 16000)
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
        writer.Write(16);
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

    /// <summary>
    /// Write float32 PCM samples as a 16-bit mono WAV file to disk.
    /// Returns the temp file path.
    /// </summary>
    public static string WriteWavToTempFile(float[] samples, int sampleRate = 16000)
    {
        var path = Path.Combine(Path.GetTempPath(), $"supervibe-{Guid.NewGuid():N}.wav");
        using var fs = File.Create(path);
        WriteWavToStream(fs, samples, sampleRate);
        return path;
    }
}

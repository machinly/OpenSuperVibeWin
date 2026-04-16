using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperVibe.Services;

public interface ISttEngine : IDisposable
{
    string Name { get; }
    bool IsAvailable { get; }
    Task EnsureModelLoadedAsync(CancellationToken ct = default);
    Task<string> TranscribeAsync(float[] audioBuffer, CancellationToken ct = default);
}

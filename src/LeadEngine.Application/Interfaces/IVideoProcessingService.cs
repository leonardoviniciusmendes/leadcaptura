using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IVideoProcessingService
{
    Task<VideoMetadata?> ProbeAsync(string path, CancellationToken cancellationToken);
    Task<string?> ExtractPosterAsync(string videoPath, string outputPath, double durationSeconds, CancellationToken cancellationToken);
    Task<IReadOnlyList<CreativeAssetAnalysisFrame>> ExtractAnalysisFramesAsync(string videoPath, double durationSeconds, CancellationToken cancellationToken);
}

public sealed record VideoMetadata(
    double DurationSeconds,
    int Width,
    int Height,
    string? Codec,
    double? FrameRate,
    int? Rotation);

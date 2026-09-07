using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class FfmpegVideoProcessingService(
    IOptions<VideoProcessingOptions> options,
    ILogger<FfmpegVideoProcessingService> logger) : IVideoProcessingService
{
    public async Task<VideoMetadata?> ProbeAsync(string path, CancellationToken cancellationToken)
    {
        var config = EffectiveOptions();
        try
        {
            var result = await RunAsync(config.FfprobePath, [
                "-v", "error",
                "-select_streams", "v:0",
                "-show_entries", "stream=width,height,codec_name,r_frame_rate:stream_tags=rotate:format=duration",
                "-of", "json",
                path
            ], config.TimeoutSeconds, cancellationToken);

            if (result.ExitCode != 0)
            {
                logger.LogWarning("ffprobe failed. ExitCode={ExitCode}", result.ExitCode);
                return null;
            }

            using var doc = JsonDocument.Parse(result.StandardOutput);
            var root = doc.RootElement;
            var stream = root.TryGetProperty("streams", out var streams) && streams.GetArrayLength() > 0
                ? streams[0]
                : default;
            if (stream.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            var duration = root.GetProperty("format").TryGetProperty("duration", out var durationElement)
                && double.TryParse(durationElement.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var durationValue)
                ? durationValue
                : 0;
            var width = stream.GetProperty("width").GetInt32();
            var height = stream.GetProperty("height").GetInt32();
            var rotation = stream.TryGetProperty("tags", out var tags)
                && tags.TryGetProperty("rotate", out var rotate)
                && int.TryParse(rotate.GetString(), out var rotateValue)
                    ? rotateValue
                    : (int?)null;
            var frameRate = stream.TryGetProperty("r_frame_rate", out var rate)
                ? ParseFrameRate(rate.GetString())
                : null;
            var codec = stream.TryGetProperty("codec_name", out var codecElement) ? codecElement.GetString() : null;
            if (rotation is 90 or 270)
            {
                (width, height) = (height, width);
            }

            logger.LogInformation("ffprobe video metadata. Duration={Duration} Width={Width} Height={Height} Codec={Codec} FrameRate={FrameRate} Rotation={Rotation}", duration, width, height, codec, frameRate, rotation);
            return duration > 0 && width > 0 && height > 0
                ? new VideoMetadata(duration, width, height, codec, frameRate, rotation)
                : null;
        }
        catch (Exception ex) when (ex is FileNotFoundException or System.ComponentModel.Win32Exception)
        {
            logger.LogWarning("ffprobe unavailable. Path={FfprobePath}", config.FfprobePath);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "ffprobe metadata extraction failed.");
            return null;
        }
    }

    public async Task<string?> ExtractPosterAsync(string videoPath, string outputPath, double durationSeconds, CancellationToken cancellationToken)
    {
        var config = EffectiveOptions();
        foreach (var offset in CandidateOffsets(durationSeconds, [0.10, 0.25, 0.50]))
        {
            if (await ExtractFrameAsync(config, videoPath, outputPath, offset, cancellationToken))
            {
                logger.LogInformation("ffmpeg poster extracted. Timestamp={Timestamp}", offset);
                return outputPath;
            }
        }

        logger.LogWarning("ffmpeg poster extraction failed for all timestamps.");
        return null;
    }

    public async Task<IReadOnlyList<CreativeAssetAnalysisFrame>> ExtractAnalysisFramesAsync(string videoPath, double durationSeconds, CancellationToken cancellationToken)
    {
        var config = EffectiveOptions();
        var root = Path.Combine(Path.GetTempPath(), $"leadengine-video-frames-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var frames = new List<CreativeAssetAnalysisFrame>();
            foreach (var (offset, index) in CandidateOffsets(durationSeconds, [0.10, 0.30, 0.50, 0.70, 0.90]).Take(Math.Max(config.MaxFramesForAnalysis, 1)).Select((x, i) => (x, i)))
            {
                var framePath = Path.Combine(root, $"frame-{index + 1}.jpg");
                if (!await ExtractFrameAsync(config, videoPath, framePath, offset, cancellationToken))
                {
                    continue;
                }

                frames.Add(new CreativeAssetAnalysisFrame($"frame-{index + 1}", offset, "image/jpeg", await File.ReadAllBytesAsync(framePath, cancellationToken)));
            }

            logger.LogInformation("ffmpeg analysis frames extracted. Count={Count} Timestamps={Timestamps}", frames.Count, string.Join(",", frames.Select(x => x.OffsetSeconds.ToString("0.##", CultureInfo.InvariantCulture))));
            return frames;
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Could not clean temporary video frame directory.");
            }
        }
    }

    private async Task<bool> ExtractFrameAsync(VideoProcessingOptions config, string videoPath, string outputPath, double offsetSeconds, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RunAsync(config.FfmpegPath, [
                "-y",
                "-ss", offsetSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                "-i", videoPath,
                "-frames:v", "1",
                "-vf", $"scale=min({config.FrameWidth}\\,iw):-2",
                "-q:v", "2",
                outputPath
            ], config.TimeoutSeconds, cancellationToken);

            return result.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        }
        catch (Exception ex) when (ex is FileNotFoundException or System.ComponentModel.Win32Exception)
        {
            logger.LogWarning("ffmpeg unavailable. Path={FfmpegPath}", config.FfmpegPath);
            return false;
        }
    }

    private static async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, int timeoutSeconds, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 1)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(linked.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(linked.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private VideoProcessingOptions EffectiveOptions()
    {
        var value = options.Value;
        return new VideoProcessingOptions
        {
            FfmpegPath = Environment.GetEnvironmentVariable("VIDEO_PROCESSING_FFMPEG_PATH") ?? value.FfmpegPath,
            FfprobePath = Environment.GetEnvironmentVariable("VIDEO_PROCESSING_FFPROBE_PATH") ?? value.FfprobePath,
            MaxFramesForAnalysis = int.TryParse(Environment.GetEnvironmentVariable("VIDEO_PROCESSING_MAX_FRAMES_FOR_ANALYSIS"), out var maxFrames) ? maxFrames : value.MaxFramesForAnalysis,
            FrameWidth = int.TryParse(Environment.GetEnvironmentVariable("VIDEO_PROCESSING_FRAME_WIDTH"), out var frameWidth) ? frameWidth : value.FrameWidth,
            TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("VIDEO_PROCESSING_TIMEOUT_SECONDS"), out var timeout) ? timeout : value.TimeoutSeconds
        };
    }

    private static IReadOnlyList<double> CandidateOffsets(double durationSeconds, IReadOnlyList<double> ratios)
    {
        var duration = Math.Max(durationSeconds, 1);
        return ratios
            .Select(x => Math.Clamp(duration * x, 0.1, Math.Max(duration - 0.1, 0.1)))
            .DistinctBy(x => Math.Round(x, 2))
            .ToArray();
    }

    private static double? ParseFrameRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split('/');
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator)
            && denominator != 0)
        {
            return numerator / denominator;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var direct) ? direct : null;
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}

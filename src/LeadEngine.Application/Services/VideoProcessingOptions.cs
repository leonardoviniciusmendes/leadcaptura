namespace LeadEngine.Application.Services;

public sealed class VideoProcessingOptions
{
    public string FfmpegPath { get; set; } = "ffmpeg";
    public string FfprobePath { get; set; } = "ffprobe";
    public int MaxFramesForAnalysis { get; set; } = 5;
    public int FrameWidth { get; set; } = 768;
    public int TimeoutSeconds { get; set; } = 30;
}

namespace LeadEngine.Application.Services;

public sealed class CreativeAssetOptions
{
    public string StorageRoot { get; set; } = "storage/creative-assets";
    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
    public long MaxImageBytes { get; set; } = 10 * 1024 * 1024;
    public long MaxVideoBytes { get; set; } = 100 * 1024 * 1024;
    public int MaxVideoDurationSeconds { get; set; } = 120;
    public int MaxWidth { get; set; } = 10000;
    public int MaxHeight { get; set; } = 10000;
}

public sealed class CreativeAnalysisOptions
{
    public const string DefaultModel = "openrouter/auto";
    public const string DefaultCostTier = "low";

    public string Provider { get; set; } = "OpenRouter";
    public string Model { get; set; } = DefaultModel;
    public string CostTier { get; set; } = DefaultCostTier;
    public bool FallbackToFake { get; set; }
}

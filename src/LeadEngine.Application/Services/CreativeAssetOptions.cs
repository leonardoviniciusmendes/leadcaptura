namespace LeadEngine.Application.Services;

public sealed class CreativeAssetOptions
{
    public string StorageRoot { get; set; } = "storage/creative-assets";
    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxWidth { get; set; } = 10000;
    public int MaxHeight { get; set; } = 10000;
}

public sealed class CreativeAnalysisOptions
{
    public string Provider { get; set; } = "Fake";
    public string Model { get; set; } = string.Empty;
    public bool FallbackToFake { get; set; }
}

namespace LeadEngine.Domain.Entities;

public sealed class CreativeAsset
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    public bool IsSelected { get; set; }
    public DateTime CreatedAt { get; set; }
    public Campanha Campaign { get; set; } = null!;
    public ICollection<CreativeAssetAnalysis> Analyses { get; set; } = [];
}

namespace LeadEngine.Domain.Entities;

public sealed class Segment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string? DefaultConfigJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Campanha> Campanhas { get; set; } = [];
}

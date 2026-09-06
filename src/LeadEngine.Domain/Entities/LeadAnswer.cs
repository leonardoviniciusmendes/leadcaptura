namespace LeadEngine.Domain.Entities;

public sealed class LeadAnswer
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string LabelSnapshot { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ValueJson { get; set; } = string.Empty;
    public Lead Lead { get; set; } = null!;
}

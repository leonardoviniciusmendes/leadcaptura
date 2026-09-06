namespace LeadEngine.Domain.Entities;

public sealed class LeadFormField
{
    public Guid Id { get; set; }
    public Guid LeadFormId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? OptionsJson { get; set; }
    public string? ValidationJson { get; set; }
    public int Order { get; set; }
    public string? DefaultValue { get; set; }
    public LeadForm LeadForm { get; set; } = null!;
}

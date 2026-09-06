namespace LeadEngine.Domain.Entities;

public sealed class LeadForm
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public int Version { get; set; }
    public string SubmitButtonText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Campanha Campaign { get; set; } = null!;
    public ICollection<LeadFormField> Fields { get; set; } = [];
}

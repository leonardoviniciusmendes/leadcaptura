namespace LeadEngine.Domain.Entities;

public sealed class MetaAdsOAuthState
{
    public Guid Id { get; set; }
    public string StateHash { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public bool Utilizado { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUtilizacao { get; set; }
}

namespace LeadEngine.Domain.Entities;

public sealed class MetaAdsConta
{
    public Guid Id { get; set; }
    public string MetaUserId { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public bool Ativa { get; set; }
    public DateTime DataConexao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public string? AccessTokenProtegido { get; set; }
    public string? TokenType { get; set; }
    public DateTime? AccessTokenExpiraEm { get; set; }
    public MetaAdsAtivoSelecionado? AtivoSelecionado { get; set; }
}

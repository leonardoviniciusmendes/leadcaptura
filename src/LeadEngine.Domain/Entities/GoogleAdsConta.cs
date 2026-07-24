namespace LeadEngine.Domain.Entities;

public sealed class GoogleAdsConta
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Ativa { get; set; }
    public bool Padrao { get; set; }
    public DateTime DataConexao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public string? AccessTokenProtegido { get; set; }
    public string? RefreshTokenProtegido { get; set; }
    public DateTime? AccessTokenExpiraEm { get; set; }
}

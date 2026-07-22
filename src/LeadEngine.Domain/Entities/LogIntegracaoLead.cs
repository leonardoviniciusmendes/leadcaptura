namespace LeadEngine.Domain.Entities;

public class LogIntegracaoLead
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool Sucesso { get; set; }
    public int? StatusHttp { get; set; }
    public string? Mensagem { get; set; }
    public string? Endpoint { get; set; }
    public Lead Lead { get; set; } = null!;
}

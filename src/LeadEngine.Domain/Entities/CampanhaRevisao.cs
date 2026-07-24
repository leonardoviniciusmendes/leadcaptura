using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class CampanhaRevisao
{
    public Guid Id { get; set; }
    public Guid CampanhaId { get; set; }
    public string TipoAlteracao { get; set; } = string.Empty;
    public CampanhaSecao? Secao { get; set; }
    public string ConteudoAnterior { get; set; } = "{}";
    public string ConteudoNovo { get; set; } = "{}";
    public OrigemRevisaoCampanha Origem { get; set; }
    public string? InstrucaoAdicional { get; set; }
    public string? ProviderIa { get; set; }
    public string? ModeloIa { get; set; }
    public DateTime DataAlteracao { get; set; }
    public Campanha? Campanha { get; set; }
}

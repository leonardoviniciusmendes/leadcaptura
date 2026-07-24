using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class ConfiguracaoSistemaHistorico
{
    public Guid Id { get; set; }
    public Guid ConfiguracaoSistemaId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public CategoriaConfiguracao Categoria { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNovo { get; set; }
    public bool Sensivel { get; set; }
    public DateTime DataAlteracao { get; set; }
    public string OrigemAlteracao { get; set; } = "Interface";
    public ConfiguracaoSistema? ConfiguracaoSistema { get; set; }
}

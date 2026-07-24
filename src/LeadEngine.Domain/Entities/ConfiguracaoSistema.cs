using LeadEngine.Domain.Enums;

namespace LeadEngine.Domain.Entities;

public sealed class ConfiguracaoSistema
{
    public Guid Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public CategoriaConfiguracao Categoria { get; set; }
    public string? Valor { get; set; }
    public string? ValorProtegido { get; set; }
    public bool Sensivel { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public string? Descricao { get; set; }
    public ICollection<ConfiguracaoSistemaHistorico> Historico { get; set; } = [];
}

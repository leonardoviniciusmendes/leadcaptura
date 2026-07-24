using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/configuracoes")]
public sealed class ConfiguracoesController(IConfiguracaoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConfiguracaoCategoriaResponse>>> Listar(CancellationToken cancellationToken)
    {
        return Ok(await service.ListarAsync(cancellationToken));
    }

    [HttpGet("{categoria}")]
    public async Task<ActionResult<ConfiguracaoCategoriaResponse>> Obter(CategoriaConfiguracao categoria, CancellationToken cancellationToken)
    {
        return Ok(await service.ObterCategoriaAsync(categoria, cancellationToken));
    }

    [HttpPut("{categoria}")]
    public async Task<ActionResult<ConfiguracaoCategoriaResponse>> Atualizar(CategoriaConfiguracao categoria, JsonElement request, CancellationToken cancellationToken)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, object?>>(request.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return Ok(await service.AtualizarCategoriaAsync(categoria, values, cancellationToken));
    }

    [HttpPost("{categoria}/testar")]
    public async Task<ActionResult<TesteConfiguracaoResponse>> Testar(CategoriaConfiguracao categoria, CancellationToken cancellationToken)
    {
        return Ok(await service.TestarAsync(categoria, cancellationToken));
    }

    [HttpGet("status")]
    public async Task<ActionResult<ConfiguracoesStatusResponse>> Status(CancellationToken cancellationToken)
    {
        return Ok(await service.ObterStatusAsync(cancellationToken));
    }

    [HttpGet("historico")]
    public async Task<ActionResult<IReadOnlyList<ConfiguracaoHistoricoResponse>>> Historico(
        [FromQuery] CategoriaConfiguracao? categoria,
        [FromQuery] string? chave,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListarHistoricoAsync(new ConfiguracaoHistoricoQuery(categoria, chave, dataInicial, dataFinal), cancellationToken));
    }
}

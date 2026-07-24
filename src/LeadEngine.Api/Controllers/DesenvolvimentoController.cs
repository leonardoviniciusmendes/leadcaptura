using LeadEngine.Infrastructure.CampaignGeneration;
using Microsoft.AspNetCore.Mvc;

namespace LeadEngine.Api.Controllers;

[ApiController]
[Route("api/desenvolvimento")]
public sealed class DesenvolvimentoController(
    IWebHostEnvironment environment,
    OpenRouterCampaignGenerationService openRouter) : ControllerBase
{
    [HttpPost("openrouter/testar")]
    public async Task<ActionResult<object>> TestarOpenRouter(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await openRouter.TestarConectividadeAsync(cancellationToken);
        return Ok(new
        {
            provider = result.Provider,
            modelo = result.Modelo,
            duracaoMs = result.DuracaoMs
        });
    }
}

using Korp.Stock.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Stock.Api.Controllers;

[ApiController]
[Route("api")]
public class OpsController(ChaosState chaos) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new
    {
        service = "stock",
        status = chaos.Enabled ? "degraded" : "healthy",
        chaos = chaos.Enabled,
        timestamp = DateTimeOffset.UtcNow
    });

    [HttpGet("chaos")]
    public IActionResult GetChaos() => Ok(new { enabled = chaos.Enabled, message = chaos.Enabled ? "Falha simulada ativa" : "Serviço operando normalmente" });

    [HttpPost("chaos")]
    public IActionResult SetChaos([FromBody] ChaosToggleRequest request)
    {
        chaos.Enabled = request.Enabled;
        return Ok(new
        {
            enabled = chaos.Enabled,
            message = chaos.Enabled
                ? "Microsserviço de estoque em falha simulada. Chamadas de negócio retornam HTTP 503."
                : "Microsserviço de estoque restaurado."
        });
    }
}

public record ChaosToggleRequest(bool Enabled);

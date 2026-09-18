using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Billing.Api.Controllers;

[ApiController]
[Route("api")]
public class OpsController(AssistantService assistant) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new
    {
        service = "billing",
        status = "healthy",
        timestamp = DateTimeOffset.UtcNow
    });

    [HttpGet("ai/insights")]
    public async Task<ActionResult<IReadOnlyList<InsightResponse>>> Insights(CancellationToken ct) =>
        Ok(await assistant.InsightsAsync(ct));

    [HttpPost("ai/ask")]
    public async Task<ActionResult<AssistantResponse>> Ask([FromBody] AssistantRequest request, CancellationToken ct) =>
        Ok(await assistant.AskAsync(request.Question, ct));
}

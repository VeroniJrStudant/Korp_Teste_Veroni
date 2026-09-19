using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Stock.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController(CatalogAiService ai) : ControllerBase
{
    [HttpPost("suggest-description")]
    public ActionResult<SuggestDescriptionResponse> Suggest([FromBody] SuggestDescriptionRequest request) =>
        Ok(ai.Suggest(request));
}

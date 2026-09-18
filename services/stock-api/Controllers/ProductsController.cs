using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Stock.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List([FromQuery] string? search, CancellationToken ct) =>
        Ok(await products.ListAsync(search, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await products.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var created = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        Ok(await products.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/debit")]
    public async Task<ActionResult<StockOperationResponse>> Debit(Guid id, [FromBody] StockOperationRequest request, CancellationToken ct) =>
        Ok(await products.DebitAsync(id, request, ct));

    [HttpPost("{id:guid}/credit")]
    public async Task<ActionResult<StockOperationResponse>> Credit(Guid id, [FromBody] StockOperationRequest request, CancellationToken ct) =>
        Ok(await products.CreditAsync(id, request, ct));
}

using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Billing.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController(InvoiceService invoices) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceResponse>>> List(CancellationToken ct) =>
        Ok(await invoices.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await invoices.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<InvoiceResponse>> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var created = await invoices.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> Update(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken ct) =>
        Ok(await invoices.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/print")]
    public async Task<ActionResult<PrintInvoiceResponse>> Print(Guid id, CancellationToken ct)
    {
        var key = Request.Headers["Idempotency-Key"].FirstOrDefault();
        return Ok(await invoices.PrintAsync(id, key, ct));
    }
}

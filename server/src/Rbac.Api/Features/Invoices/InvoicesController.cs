using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Authorization;
using Rbac.Api.Common;

namespace Rbac.Api.Features.Invoices;

[ApiController]
[Route("api/invoices")]
[Authorize]
public sealed class InvoicesController(InvoiceService invoiceService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Invoices.View)]
    public Task<IReadOnlyList<InvoiceResponse>> List([FromQuery] InvoiceQuery query, CancellationToken cancellationToken) =>
        invoiceService.ListAsync(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<ActionResult<InvoiceResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await invoiceService.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [HasPermission(Permissions.Invoices.Create)]
    public async Task<ActionResult<InvoiceResponse>> Create(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await invoiceService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Invoices.Edit)]
    public async Task<ActionResult<InvoiceResponse>> Update(
        Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken) =>
        (await invoiceService.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Invoices.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await invoiceService.DeleteAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/submit")]
    [HasPermission(Permissions.Invoices.Submit)]
    public async Task<ActionResult<InvoiceResponse>> Submit(Guid id, CancellationToken cancellationToken) =>
        (await invoiceService.SubmitAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/approve")]
    [HasPermission(Permissions.Invoices.Approve)]
    public async Task<ActionResult<InvoiceResponse>> Approve(Guid id, CancellationToken cancellationToken) =>
        (await invoiceService.ApproveAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.Invoices.Approve)]
    public async Task<ActionResult<InvoiceResponse>> Reject(Guid id, CancellationToken cancellationToken) =>
        (await invoiceService.RejectAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>Two attributes = both permissions are required (AND).</summary>
    [HttpGet("export")]
    [HasPermission(Permissions.Invoices.View)]
    [HasPermission(Permissions.Invoices.Export)]
    public async Task<FileContentResult> Export([FromQuery] InvoiceQuery query, CancellationToken cancellationToken) =>
        File(await invoiceService.ExportCsvAsync(query, cancellationToken), "text/csv", "invoices.csv");
}

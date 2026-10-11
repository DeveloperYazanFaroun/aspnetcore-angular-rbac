using System.ComponentModel.DataAnnotations;
using Rbac.Api.Domain;

namespace Rbac.Api.Features.Invoices;

public sealed record InvoiceResponse(
    Guid Id,
    string Number,
    string CustomerName,
    decimal Amount,
    InvoiceStatus Status,
    Guid CreatedById,
    string CreatedByName,
    DateTime CreatedAtUtc,
    Guid? ReviewedById,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc);

public sealed record InvoiceQuery(InvoiceStatus? Status, string? Search);

public sealed record CreateInvoiceRequest(
    [Required, StringLength(200, MinimumLength = 2)] string CustomerName,
    [Range(typeof(decimal), "0.01", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] decimal Amount);

public sealed record UpdateInvoiceRequest(
    [Required, StringLength(200, MinimumLength = 2)] string CustomerName,
    [Range(typeof(decimal), "0.01", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] decimal Amount);

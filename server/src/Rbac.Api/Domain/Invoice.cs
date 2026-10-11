using Rbac.Api.Common;

namespace Rbac.Api.Domain;

/// <summary>
/// The sample business entity. It shows the authorization rules that a permission alone can't express:
/// status rules and separation of duties (the creator of an invoice can't approve it).
/// </summary>
public sealed class Invoice : ITenantOwned
{
    private Invoice()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid TenantId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public string CustomerName { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public Guid CreatedById { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? ReviewedById { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public static Invoice CreateDraft(
        Guid tenantId, string number, string customerName, decimal amount, Guid createdById, DateTime nowUtc) =>
        new()
        {
            TenantId = tenantId,
            Number = number,
            CustomerName = customerName.Trim(),
            Amount = amount,
            Status = InvoiceStatus.Draft,
            CreatedById = createdById,
            CreatedAtUtc = nowUtc,
        };

    public Result Update(string customerName, decimal amount)
    {
        if (Status != InvoiceStatus.Draft)
        {
            return Error.Conflict($"Invoice {Number} is {Status}. Only draft invoices can be edited.");
        }

        CustomerName = customerName.Trim();
        Amount = amount;
        return Result.Success();
    }

    public Result EnsureCanBeDeleted() =>
        Status == InvoiceStatus.Draft
            ? Result.Success()
            : Error.Conflict($"Invoice {Number} is {Status}. Only draft invoices can be deleted.");

    public Result Submit()
    {
        if (Status != InvoiceStatus.Draft)
        {
            return Error.Conflict($"Invoice {Number} is {Status}. Only draft invoices can be submitted.");
        }

        Status = InvoiceStatus.Submitted;
        return Result.Success();
    }

    public Result Approve(Guid reviewerId, DateTime nowUtc) => Review(reviewerId, nowUtc, InvoiceStatus.Approved);

    public Result Reject(Guid reviewerId, DateTime nowUtc) => Review(reviewerId, nowUtc, InvoiceStatus.Rejected);

    private Result Review(Guid reviewerId, DateTime nowUtc, InvoiceStatus decision)
    {
        if (Status != InvoiceStatus.Submitted)
        {
            return Error.Conflict($"Invoice {Number} is {Status}. Only submitted invoices can be reviewed.");
        }

        // Separation of duties: the person who created the invoice must not review it.
        if (reviewerId == CreatedById)
        {
            return Error.Forbidden("You can't review an invoice you created. Another approver must review it.");
        }

        Status = decision;
        ReviewedById = reviewerId;
        ReviewedAtUtc = nowUtc;
        return Result.Success();
    }
}

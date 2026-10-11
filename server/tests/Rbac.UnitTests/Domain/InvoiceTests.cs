using Rbac.Api.Common;
using Rbac.Api.Domain;

namespace Rbac.UnitTests.Domain;

public sealed class InvoiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CreatorId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creator_cannot_approve_their_own_invoice()
    {
        var invoice = SubmittedInvoice();

        var result = invoice.Approve(CreatorId, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        Assert.Equal(InvoiceStatus.Submitted, invoice.Status);
    }

    [Fact]
    public void Another_user_can_approve_a_submitted_invoice()
    {
        var invoice = SubmittedInvoice();

        var result = invoice.Approve(ApproverId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal(ApproverId, invoice.ReviewedById);
    }

    [Fact]
    public void Draft_invoice_cannot_be_approved()
    {
        var invoice = Draft();

        var result = invoice.Approve(ApproverId, Now);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public void Submitted_invoice_cannot_be_edited_or_deleted()
    {
        var invoice = SubmittedInvoice();

        Assert.Equal(ErrorType.Conflict, invoice.Update("New customer", 10m).Error!.Type);
        Assert.Equal(ErrorType.Conflict, invoice.EnsureCanBeDeleted().Error!.Type);
    }

    private static Invoice Draft() => Invoice.CreateDraft(TenantId, "INV-0001", "Northwind", 100m, CreatorId, Now);

    private static Invoice SubmittedInvoice()
    {
        var invoice = Draft();
        invoice.Submit();
        return invoice;
    }
}

using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Authorization;
using Rbac.Api.Common;
using Rbac.Api.Data;
using Rbac.Api.Domain;

namespace Rbac.Api.Features.Invoices;

/// <summary>
/// Invoice use cases. Three authorization layers meet here:
/// the controller checked the permission, the tenant query filter limits the rows,
/// and the domain enforces status rules and separation of duties.
/// </summary>
public sealed class InvoiceService(AppDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<InvoiceResponse>> ListAsync(InvoiceQuery query, CancellationToken cancellationToken)
    {
        var invoices = dbContext.Invoices.AsNoTracking();

        if (query.Status is { } status)
        {
            invoices = invoices.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            invoices = invoices.Where(i => i.Number.Contains(term) || i.CustomerName.Contains(term));
        }

        return await Project(invoices.OrderByDescending(i => i.CreatedAtUtc)).ToListAsync(cancellationToken);
    }

    public async Task<Result<InvoiceResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        // The tenant filter applies here: an invoice of another tenant is simply "not found".
        // Returning 404 (not 403) avoids confirming that the record exists.
        var invoice = await Project(dbContext.Invoices.AsNoTracking().Where(i => i.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound(id);
        }

        return invoice;
    }

    public async Task<Result<InvoiceResponse>> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.RequiredTenantId;
        var number = await NextNumberAsync(cancellationToken);

        var invoice = Invoice.CreateDraft(
            tenantId, number, request.CustomerName, request.Amount, currentUser.RequiredUserId, Now());

        dbContext.Invoices.Add(invoice);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique (TenantId, Number) index rejected a number taken by a parallel request.
            return Error.Conflict($"Invoice number {number} was just used by another invoice. Please try again.");
        }

        return await GetAsync(invoice.Id, cancellationToken);
    }

    public Task<Result<InvoiceResponse>> UpdateAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(id, invoice => invoice.Update(request.CustomerName, request.Amount), cancellationToken);

    public Task<Result<InvoiceResponse>> SubmitAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, invoice => invoice.Submit(), cancellationToken);

    public Task<Result<InvoiceResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, invoice => invoice.Approve(currentUser.RequiredUserId, Now()), cancellationToken);

    public Task<Result<InvoiceResponse>> RejectAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, invoice => invoice.Reject(currentUser.RequiredUserId, Now()), cancellationToken);

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound(id);
        }

        var check = invoice.EnsureCanBeDeleted();
        if (!check.IsSuccess)
        {
            return check;
        }

        dbContext.Invoices.Remove(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<byte[]> ExportCsvAsync(InvoiceQuery query, CancellationToken cancellationToken)
    {
        var invoices = await ListAsync(query, cancellationToken);
        var csv = new StringBuilder("Number,Customer,Amount,Status,CreatedBy,CreatedAtUtc").AppendLine();

        foreach (var i in invoices)
        {
            csv.Append(CultureInfo.InvariantCulture,
                    $"{i.Number},{CsvField(i.CustomerName)},{i.Amount},{i.Status},")
                .Append(CultureInfo.InvariantCulture, $"{CsvField(i.CreatedByName)},{i.CreatedAtUtc:O}")
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>Quotes a value and blocks spreadsheet formula injection (=, +, -, @).</summary>
    private static string CsvField(string value)
    {
        var safe = value.Length > 0 && "=+-@".Contains(value[0]) ? "'" + value : value;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private async Task<Result<InvoiceResponse>> ChangeAsync(
        Guid id, Func<Invoice, Result> change, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound(id);
        }

        var result = change(invoice);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Manual mapping to the response, including the names of the users involved.</summary>
    private IQueryable<InvoiceResponse> Project(IQueryable<Invoice> invoices) =>
        from invoice in invoices
        join creator in dbContext.Users on invoice.CreatedById equals creator.Id
        join reviewer in dbContext.Users on invoice.ReviewedById equals (Guid?)reviewer.Id into reviewers
        from reviewer in reviewers.DefaultIfEmpty()
        select new InvoiceResponse(
            invoice.Id,
            invoice.Number,
            invoice.CustomerName,
            invoice.Amount,
            invoice.Status,
            invoice.CreatedById,
            creator.FullName,
            invoice.CreatedAtUtc,
            invoice.ReviewedById,
            reviewer == null ? null : reviewer.FullName,
            invoice.ReviewedAtUtc);

    /// <summary>Highest existing number + 1 (tenant filter applies). Deleted drafts never cause reuse.</summary>
    private async Task<string> NextNumberAsync(CancellationToken cancellationToken)
    {
        var numbers = await dbContext.Invoices.Select(i => i.Number).ToListAsync(cancellationToken);
        var highest = numbers
            .Select(n => n.StartsWith(NumberPrefix, StringComparison.Ordinal) && int.TryParse(n.AsSpan(NumberPrefix.Length), out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"{NumberPrefix}{highest + 1:D4}";
    }

    private const string NumberPrefix = "INV-";

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;

    private static Error NotFound(Guid id) => Error.NotFound($"Invoice '{id}' was not found.");
}

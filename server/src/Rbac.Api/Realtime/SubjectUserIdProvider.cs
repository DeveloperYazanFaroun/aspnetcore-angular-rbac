using Microsoft.AspNetCore.SignalR;
using Rbac.Api.Authorization;

namespace Rbac.Api.Realtime;

/// <summary>
/// SignalR identifies users by the NameIdentifier claim by default. Our tokens use "sub",
/// so <c>Clients.User(id)</c> needs this provider.
/// </summary>
public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(AppClaimTypes.Subject)?.Value;
}

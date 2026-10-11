using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Rbac.Api.Realtime;

/// <summary>Messages the server can push to a connected client.</summary>
public interface INotificationsClient
{
    /// <summary>Your roles or permissions changed: reload them.</summary>
    Task PermissionsChanged();
}

/// <summary>
/// Real-time channel used to tell clients that their permissions changed.
/// MVC filters such as [HasPermission] don't run for hubs: check permissions inside hub methods if you add any.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub<INotificationsClient>
{
    public const string Path = "/hubs/notifications";
}

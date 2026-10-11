using Microsoft.AspNetCore.SignalR;
using Rbac.Api.Authorization;

namespace Rbac.Api.Realtime;

public interface IPermissionChangeNotifier
{
    /// <summary>Clears cached permissions and tells the affected users' browsers to reload them.</summary>
    Task NotifyAsync(IReadOnlyCollection<Guid> userIds);
}

public sealed class PermissionChangeNotifier(
    IPermissionService permissionService,
    IHubContext<NotificationsHub, INotificationsClient> hubContext) : IPermissionChangeNotifier
{
    public async Task NotifyAsync(IReadOnlyCollection<Guid> userIds)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        permissionService.Invalidate(userIds);
        await hubContext.Clients.Users(userIds.Select(id => id.ToString()).ToList()).PermissionsChanged();
    }
}

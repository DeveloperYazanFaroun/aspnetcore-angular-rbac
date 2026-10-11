using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rbac.Api.Authentication;
using Rbac.Api.Common;
using Rbac.Api.Data;
using Rbac.Api.Domain;

namespace Rbac.Api.Features.Auth;

/// <summary>
/// Sign-in, refresh token rotation and sign-out.
/// </summary>
public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider,
    ILogger<AuthService> logger)
{
    // One message for every sign-in failure, so attackers can't tell which emails exist.
    private static readonly Error InvalidCredentials = Error.Unauthorized("The email or password is incorrect.");
    private static readonly Error SessionExpired = Error.Unauthorized("Your session has expired. Please sign in again.");

    public async Task<Result<AuthSession>> SignInAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            // Same message as a wrong password, so a locked account doesn't confirm that the email exists.
            return InvalidCredentials;
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueSessionAsync(user, replacing: null, cancellationToken);
    }

    /// <summary>
    /// Exchanges a refresh token for a new access token and a new refresh token (rotation).
    /// If a token that was already used is presented again, it was probably stolen:
    /// every session of that user is revoked.
    /// </summary>
    public async Task<Result<AuthSession>> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return SessionExpired;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = tokenService.HashRefreshToken(refreshToken);
        var stored = await dbContext.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || stored.IsExpired(now))
        {
            return SessionExpired;
        }

        if (stored.IsRevoked)
        {
            logger.LogWarning("Refresh token reuse detected for user {UserId}. Revoking all sessions.", stored.UserId);
            await RevokeAllSessionsAsync(stored.UserId, cancellationToken);
            return SessionExpired;
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return SessionExpired;
        }

        stored.RevokedAtUtc = now;
        return await IssueSessionAsync(user, replacing: stored, cancellationToken);
    }

    public async Task SignOutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hash = tokenService.HashRefreshToken(refreshToken);
        var stored = await dbContext.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is { IsRevoked: false })
        {
            stored.RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAtUtc, (DateTime?)now), cancellationToken);
    }

    private async Task<AuthSession> IssueSessionAsync(
        ApplicationUser user, RefreshToken? replacing, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);

        var refreshValue = tokenService.CreateRefreshTokenValue();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshValue),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays),
        };

        if (replacing is not null)
        {
            replacing.ReplacedByTokenHash = refreshToken.TokenHash;
        }

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthSession(accessToken.Value, accessToken.ExpiresAtUtc, refreshValue, refreshToken.ExpiresAtUtc);
    }
}

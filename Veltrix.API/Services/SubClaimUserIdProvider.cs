using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Veltrix.API.Services;

/// <summary>
/// Tells SignalR which user a connection belongs to, so Clients.User("5") reaches user 5.
/// We keep the JWT's "sub" claim unmapped, so SignalR's default (NameIdentifier) would not find it.
/// </summary>
public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}

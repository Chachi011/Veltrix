using Veltrix.API.Hubs;
using Veltrix.Application.Common.Interfaces;

namespace Veltrix.API.Services;

/// <summary>Lets the admin dashboard (Infrastructure) read the hub's connection count without knowing SignalR.</summary>
public sealed class HubOnlineCounter : IOnlineCounter
{
    public int OnlineConnections => CrashHub.OnlineCount;
}

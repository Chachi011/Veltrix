namespace Veltrix.Application.Common.Interfaces;

/// <summary>
/// How many real-time connections are open right now (admin dashboard).
/// Implemented in the API layer, which owns the SignalR hub.
/// </summary>
public interface IOnlineCounter
{
    int OnlineConnections { get; }
}

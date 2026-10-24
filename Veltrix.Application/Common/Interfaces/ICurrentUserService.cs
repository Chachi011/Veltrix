namespace Veltrix.Application.Common.Interfaces;

/// <summary>
/// Gives services access to "who is calling" without depending on ASP.NET Core's HttpContext.
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);

    /// <summary>Operator or Admin.</summary>
    bool IsStaff { get; }

    string? IpAddress { get; }
    string? UserAgent { get; }
}

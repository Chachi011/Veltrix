using Microsoft.IdentityModel.JsonWebTokens;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User.IsInRole(role) == true;

    public bool IsStaff => IsInRole(RoleNames.Operator) || IsInRole(RoleNames.Admin);
}

using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.Interfaces;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProfileService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ProfileDto
            {
                Id = u.Id,
                Email = u.Email,
                Nickname = u.Nickname,
                Role = u.Role,
                AvatarUrl = u.AvatarUrl,
                DateOfBirth = u.DateOfBirth,
                CreatedAt = u.CreatedAt,
                Balance = u.Wallet != null ? u.Wallet.Balance : 0
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");
    }
}

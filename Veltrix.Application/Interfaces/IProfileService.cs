using Veltrix.Application.DTOs.Profile;

namespace Veltrix.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetMyProfileAsync(CancellationToken ct = default);
}

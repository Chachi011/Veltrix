using Veltrix.Application.DTOs.Profile;

namespace Veltrix.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetMyProfileAsync(CancellationToken ct = default);
    Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);

    /// <param name="content">The uploaded file bytes (already size-checked by the API layer).</param>
    Task<ProfileDto> UploadAvatarAsync(byte[] content, CancellationToken ct = default);

    Task<PublicProfileDto> GetPublicProfileAsync(string nickname, CancellationToken ct = default);

    Task<UserPreferencesDto> GetMyPreferencesAsync(CancellationToken ct = default);
    Task<UserPreferencesDto> UpdateMyPreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default);
}

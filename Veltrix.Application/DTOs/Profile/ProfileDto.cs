using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Profile;

public sealed class ProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? AvatarUrl { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Balance { get; set; }
}

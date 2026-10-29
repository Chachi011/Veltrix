namespace Veltrix.Domain.Enums;

/// <summary>
/// Guest is not stored: a guest is simply a request without a JWT.
/// </summary>
public enum UserRole
{
    Player = 1,
    Operator = 2,
    Admin = 3
}

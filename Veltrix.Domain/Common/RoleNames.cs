namespace Veltrix.Domain.Common;

/// <summary>
/// String role names used in JWT claims and [Authorize(Roles = ...)] attributes.
/// They must match the UserRole enum names.
/// </summary>
public static class RoleNames
{
    public const string Player = nameof(Enums.UserRole.Player);
    public const string Operator = nameof(Enums.UserRole.Operator);
    public const string Admin = nameof(Enums.UserRole.Admin);

    // Admin is the system owner, so it may do everything an Operator can.
    public const string OperatorOrAdmin = Operator + "," + Admin;
}

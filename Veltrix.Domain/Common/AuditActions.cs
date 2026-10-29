namespace Veltrix.Domain.Common;

/// <summary>Values of AuditLog.Action, in one place so the admin filter and the code use the same names.</summary>
public static class AuditActions
{
    public const string GameCreated = "GameCreated";
    public const string GameUpdated = "GameUpdated";
    public const string GameStatusChanged = "GameStatusChanged";
    public const string RoundCancelled = "RoundCancelled";

    public const string UserRoleChanged = "UserRoleChanged";
    public const string UserBlocked = "UserBlocked";
    public const string UserUnblocked = "UserUnblocked";
    public const string BalanceAdjusted = "BalanceAdjusted";
    public const string SettingChanged = "SettingChanged";

    public const string LimitsChanged = "LimitsChanged";
    public const string SelfExclusionStarted = "SelfExclusionStarted";

    public static readonly IReadOnlyList<string> All = new[]
    {
        GameCreated, GameUpdated, GameStatusChanged, RoundCancelled,
        UserRoleChanged, UserBlocked, UserUnblocked, BalanceAdjusted, SettingChanged,
        LimitsChanged, SelfExclusionStarted
    };
}

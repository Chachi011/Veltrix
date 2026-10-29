using System.Net;
using System.Net.Http.Json;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Domain.Enums;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>
/// Role checks with real tokens (guest 401, wrong role 403, right role 200) and the admin rules that protect
/// the admin from mistakes: no changing your own role, blocking yourself or adjusting your own balance.
/// </summary>
[Collection(ApiCollection.Name)]
public class AdminApiTests
{
    private readonly VeltrixApiFactory _api;

    public AdminApiTests(VeltrixApiFactory api) => _api = api;

    /// <summary>Every read endpoint of the admin panel.</summary>
    public static TheoryData<string> AdminEndpoints => new()
    {
        "api/admin/users",
        "api/admin/users/1",
        "api/admin/stats/overview",
        "api/admin/stats/daily",
        "api/admin/settings",
        "api/admin/audit-logs",
        "api/admin/audit-logs/actions"
    };

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Admin_endpoints_answer_401_to_guests_403_to_players_and_operators_and_200_to_admins(string url)
    {
        using var guest = _api.CreateApiClient();
        await (await guest.GetAsync(url)).ReadProblemAsync(HttpStatusCode.Unauthorized);

        foreach (var nickname in new[] { "player1", "operator" })
        {
            using var client = _api.ClientFor(await _api.LoginSeededAsync(nickname));
            await (await client.GetAsync(url)).ReadProblemAsync(HttpStatusCode.Forbidden);
        }

        using var admin = _api.ClientFor(await _api.LoginSeededAsync("admin"));
        await (await admin.GetAsync(url)).ShouldBeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Only_operators_and_admins_can_cancel_a_round()
    {
        var round = await _api.OpenBettingRoundAsync();
        var request = new CancelRoundRequest { Reason = "Integration test: role check" };

        using var guest = _api.CreateApiClient();
        await (await guest.PostAsJsonAsync($"api/rounds/{round.Id}/cancel", request, Json)).ReadProblemAsync(HttpStatusCode.Unauthorized);

        using var player = _api.ClientFor(await _api.LoginSeededAsync("player1"));
        await (await player.PostAsJsonAsync($"api/rounds/{round.Id}/cancel", request, Json)).ReadProblemAsync(HttpStatusCode.Forbidden);

        using var admin = _api.ClientFor(await _api.LoginSeededAsync("admin"));
        var cancelled = await (await admin.PostAsJsonAsync($"api/rounds/{round.Id}/cancel", request, Json))
            .ReadAsync<RoundCancellationDto>(HttpStatusCode.OK);
        Assert.Equal(round.Id, cancelled.RoundId);
    }

    [Fact]
    public async Task An_admin_cannot_change_their_own_role_block_themselves_or_adjust_their_own_balance()
    {
        var adminAuth = await _api.LoginSeededAsync("admin");
        using var admin = _api.ClientFor(adminAuth);
        var me = adminAuth.User.Id;

        await (await admin.PutAsJsonAsync($"api/admin/users/{me}/role", new ChangeRoleRequest { Role = UserRole.Player }, Json))
            .ReadProblemAsync(HttpStatusCode.BadRequest);
        await (await admin.PutAsJsonAsync($"api/admin/users/{me}/block", new BlockUserRequest { IsBlocked = true }, Json))
            .ReadProblemAsync(HttpStatusCode.BadRequest);
        await (await admin.PostAsJsonAsync($"api/admin/users/{me}/balance-adjustment", new BalanceAdjustmentRequest { Amount = 500m, Reason = "Paying myself" }, Json))
            .ReadProblemAsync(HttpStatusCode.BadRequest);

        // Still an admin
        await (await admin.GetAsync("api/admin/users")).ShouldBeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_balance_adjustment_changes_the_wallet_writes_a_ledger_row_and_an_audit_entry()
    {
        var player = await _api.RegisterPlayerAsync();
        using var admin = _api.ClientFor(await _api.LoginSeededAsync("admin"));

        var adjustment = await (await admin.PostAsJsonAsync($"api/admin/users/{player.User.Id}/balance-adjustment",
                new BalanceAdjustmentRequest { Amount = -250m, Reason = "Integration test correction" }, Json))
            .ReadAsync<BalanceAdjustmentDto>(HttpStatusCode.OK);
        Assert.Equal(1000m, adjustment.BalanceBefore);
        Assert.Equal(750m, adjustment.BalanceAfter);

        using var playerClient = _api.ClientFor(player);
        var ledger = await (await playerClient.GetAsync("api/wallet/transactions")).ReadAsync<PagedResult<WalletTransactionDto>>(HttpStatusCode.OK);
        var row = Assert.Single(ledger.Items, t => t.Type == TransactionType.AdminAdjustment);
        Assert.Equal(750m, row.BalanceAfter);

        var audit = await (await admin.GetAsync($"api/admin/audit-logs?entityType=User&entityId={player.User.Id}"))
            .ReadAsync<PagedResult<AuditLogDto>>(HttpStatusCode.OK);
        Assert.NotEmpty(audit.Items);

        // Taking more than the player has is refused: the balance can never go negative
        await (await admin.PostAsJsonAsync($"api/admin/users/{player.User.Id}/balance-adjustment",
                new BalanceAdjustmentRequest { Amount = -10_000m, Reason = "Too much" }, Json))
            .ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_promoted_player_gets_the_new_role_at_the_next_login()
    {
        var player = await _api.RegisterPlayerAsync();
        using var admin = _api.ClientFor(await _api.LoginSeededAsync("admin"));

        var details = await (await admin.PutAsJsonAsync($"api/admin/users/{player.User.Id}/role", new ChangeRoleRequest { Role = UserRole.Operator }, Json))
            .ReadAsync<AdminUserDetailsDto>(HttpStatusCode.OK);
        Assert.Equal(UserRole.Operator, details.Role);

        var again = await _api.LoginAsync(player.User.Email, PlayerPassword);
        Assert.Equal(UserRole.Operator, again.User.Role);

        // Operators do not bet
        await _api.OpenBettingRoundAsync();
        using var asOperator = _api.ClientFor(again);
        await (await asOperator.PostAsJsonAsync("api/bets", new { gameSlug = VeltrixApiFactory.CrashSlug, amount = 10m }, Json))
            .ReadProblemAsync(HttpStatusCode.Forbidden);
    }
}

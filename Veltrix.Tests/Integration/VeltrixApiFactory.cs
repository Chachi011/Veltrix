using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Veltrix.Domain.Entities;
using Veltrix.Infrastructure.Engine;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Tests.Integration;

/// <summary>All integration test classes share ONE running API and run one after another (no shared-state races).</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<VeltrixApiFactory>
{
    public const string Name = "api";
}

/// <summary>
/// Starts the REAL API in memory: the same Program.cs, middleware, JWT auth, validation, rate limits,
/// EF Core and SQL Server, called over HTTP. Differences from a normal run:
/// - a throwaway database "Veltrix_Tests_&lt;random&gt;", created by the migrations and dropped at the end;
/// - the crash engine's background loop is off, so a test moves its round through the states itself with
///   RoundSettlementService (the same code the engine uses) and nothing changes behind its back;
/// - a test JWT key (no user-secrets needed) and a clock the test can stop and move forward;
/// - every client gets its own fake IP address, so the auth rate limit (10/min/IP) counts each test separately.
/// The SQL Server instance comes from the VELTRIX_TEST_SQL environment variable (default: localhost, Windows login).
/// </summary>
public sealed class VeltrixApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ClientIpHeader = "X-Test-Client-IP";
    public const string CrashSlug = "crash";

    private const string DatabasePrefix = "Veltrix_Tests_";
    private int _clientCount;

    public VeltrixApiFactory()
    {
        DatabaseName = DatabasePrefix + Guid.NewGuid().ToString("N")[..12];
        var server = Environment.GetEnvironmentVariable("VELTRIX_TEST_SQL")
            ?? "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True;";
        ConnectionString = new SqlConnectionStringBuilder(server) { InitialCatalog = DatabaseName }.ConnectionString;
    }

    public string DatabaseName { get; }
    public string ConnectionString { get; }
    public TestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting values reach Program.cs as command-line arguments: they override appsettings.json
        // and are already there when Program.cs reads the connection string
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", "integration-tests-only-signing-key-0123456789abcdef");
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:SeedTestUsers", "true");
        builder.UseSetting("CrashEngine:Enabled", "false");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IStartupFilter, FakeClientIpStartupFilter>();
        });
    }

    public async Task InitializeAsync()
    {
        // Starts the app: migrations create the test database and the test users are seeded
        using var _ = CreateClient();

        // Safety net: never run (or drop!) anything on a database that is not this run's test database
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actual = db.Database.GetDbConnection().Database;
        if (actual != DatabaseName)
            throw new InvalidOperationException($"Integration tests are connected to '{actual}' instead of '{DatabaseName}'.");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (db.Database.GetDbConnection().Database.StartsWith(DatabasePrefix, StringComparison.Ordinal))
                await db.Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }

    /// <summary>An HTTP client with its own fake IP address, optionally logged in with this access token.</summary>
    public HttpClient CreateApiClient(string? accessToken = null)
    {
        var client = CreateClient();
        var n = Interlocked.Increment(ref _clientCount);
        client.DefaultRequestHeaders.Add(ClientIpHeader, $"10.0.{n / 250}.{n % 250 + 1}");
        if (accessToken is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>Runs code with a fresh DbContext, like a separate request would.</summary>
    public async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    /// <summary>Moves a round to its next state with the engine's own settlement code (e.g. s =&gt; s.StartAsync(id, default)).</summary>
    public async Task<T> WithSettlementAsync<T>(Func<RoundSettlementService, Task<T>> step)
    {
        await using var scope = Services.CreateAsyncScope();
        return await step(scope.ServiceProvider.GetRequiredService<RoundSettlementService>());
    }

    /// <summary>
    /// A new crash round with betting open for one minute. Its crash point is random (fixed by the seed),
    /// so rounds outside [minCrashPoint, maxCrashPoint] are cancelled and another one is created.
    /// </summary>
    public async Task<GameRound> OpenBettingRoundAsync(decimal minCrashPoint = 1m, decimal maxCrashPoint = decimal.MaxValue)
    {
        var gameId = await WithDbAsync(db => db.Games.Where(g => g.Slug == CrashSlug).Select(g => g.Id).SingleAsync());

        // Like the engine after a restart: an earlier test's unfinished round is cancelled (and refunded) first
        await WithSettlementAsync(s => s.RecoverUnfinishedRoundsAsync(default));

        while (true)
        {
            var round = await WithSettlementAsync(s => s.CreateRoundAsync(gameId, default));
            if (round.CrashPoint >= minCrashPoint && round.CrashPoint <= maxCrashPoint)
                return await WithSettlementAsync(s => s.OpenBettingAsync(round.Id, TimeSpan.FromMinutes(1), default));

            await WithSettlementAsync(s => s.CancelAsync(round.Id, null, "Test setup: crash point outside the wanted range", default));
        }
    }

    /// <summary>TestServer requests have no client IP; this takes it from the X-Test-Client-IP header instead.</summary>
    private sealed class FakeClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (IPAddress.TryParse(context.Request.Headers[ClientIpHeader].ToString(), out var address))
                    context.Connection.RemoteIpAddress = address;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}

/// <summary>
/// The real clock, but a test can stop it (Freeze) and move it forward (Advance), e.g. to cash out at an exact
/// multiplier. It never goes back: after a freeze it continues from the frozen moment.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private readonly object _sync = new();
    private TimeSpan _offset;
    private DateTimeOffset? _frozenAt;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_sync)
            return _frozenAt ?? base.GetUtcNow() + _offset;
    }

    public DateTime UtcNow => GetUtcNow().UtcDateTime;

    /// <summary>Stops the clock until the returned object is disposed.</summary>
    public IDisposable Freeze()
    {
        lock (_sync)
            _frozenAt = GetUtcNow();
        return new Thaw(this);
    }

    public void Advance(TimeSpan by)
    {
        lock (_sync)
        {
            if (_frozenAt is { } frozen)
                _frozenAt = frozen + by;
            else
                _offset += by;
        }
    }

    private void Unfreeze()
    {
        lock (_sync)
        {
            if (_frozenAt is not { } frozen)
                return;

            var ahead = frozen - base.GetUtcNow();
            if (ahead > _offset)
                _offset = ahead;
            _frozenAt = null;
        }
    }

    private sealed class Thaw(TestClock clock) : IDisposable
    {
        public void Dispose() => clock.Unfreeze();
    }
}

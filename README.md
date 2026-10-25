# VELTRIX

A real-time multiplayer web platform whose main module is a **Crash game**: a multiplier grows during a round, and players must cash out before it crashes.

> **Educational / demo project.** Only virtual **VLX points** exist. There are no deposits, purchases, withdrawals or real money. Points come only from the starting balance, the daily bonus and achievements.

## Tech stack

| Layer | Technologies |
|---|---|
| Backend | C#, ASP.NET Core Web API (.NET 8), EF Core 8 (Code First), SQL Server, SignalR, JWT + refresh tokens, BCrypt, FluentValidation, Serilog, Swagger |
| Frontend | Angular 20 (standalone, zoneless, signals), TypeScript, SCSS, NgRx SignalStore, @microsoft/signalr, HTML Canvas |
| Tests | xUnit + Moq (backend), Jasmine/Karma (frontend) |

## Architecture

```
Veltrix.API            → controllers, middleware, filters, auth/Swagger/CORS setup   (depends on Application + Infrastructure)
Veltrix.Application    → DTOs, service interfaces, validators, exceptions            (depends on Domain)
Veltrix.Infrastructure → EF Core DbContext, migrations, service implementations, JWT, BCrypt
Veltrix.Domain         → entities, enums, business rules; no external packages
Veltrix.Tests          → xUnit tests
Veltrix.Web            → Angular client
```

The dependencies point inward. The Domain project knows nothing about the database or the web, so its rules can be unit-tested in isolation.

Inside `Veltrix.Web/src/app`:

```
core/      models (DTO types), api/ (HTTP services), auth/ (AuthStore, interceptor, guards),
           http/ (error + loading interceptors), realtime/ (SignalR), state/ (WalletStore), ui/ (toasts, theme)
features/  auth, crash (CrashStore, Canvas chart, bet panel, live bets, history strip), wallet, history, profile, fairness
shared/    pipes, validators, pagination, field errors
```

## Design system

The UI follows [docs/DESIGN.md](docs/DESIGN.md): tokens, fonts, layout, components, motion and accessibility rules. In development, `/styleguide` shows every component in both themes.

## Prerequisites

- .NET SDK 8+ and the **ASP.NET Core 8 runtime** (`winget install Microsoft.DotNet.AspNetCore.8`)
- SQL Server (local default instance, Windows authentication)
- EF Core tools: `dotnet tool install --global dotnet-ef`
- Node.js 20.19+ / 22.12+ / 24 and Angular CLI 20 (`npm install -g @angular/cli@20`)

## Setup and run (backend)

```bash
# 1. JWT signing key: a secret, so it lives in user-secrets and never in Git (32+ chars)
dotnet user-secrets set "Jwt:Key" "<any-random-string-of-at-least-32-characters>" --project Veltrix.API

# 2. Run. In Development the API applies migrations and seeds test users automatically.
dotnet run --project Veltrix.API --launch-profile http
```

Swagger UI: http://localhost:5295/swagger

To apply migrations manually instead:

```bash
dotnet ef database update --project Veltrix.Infrastructure --startup-project Veltrix.API
```

## Setup and run (frontend)

With the API running:

```bash
cd Veltrix.Web
npm install
npm start          # ng serve → http://localhost:4200
```

`proxy.conf.json` forwards `/api`, `/hubs` (WebSockets) and `/uploads` to the API at `http://localhost:5295`. The browser therefore sees one origin, and the client only uses relative URLs.

Open http://localhost:4200. To demo multiplayer, use two browser windows (or one normal and one private window) logged in as `player1` and `player2`. In development, the login page has one-click buttons for the test accounts.

## Test accounts (Development seed)

| Role | Email | Password |
|---|---|---|
| Admin | admin@veltrix.dev | Admin123! |
| Operator | operator@veltrix.dev | Operator123! |
| Player | player1@veltrix.dev | Player123! |
| Player | player2@veltrix.dev | Player123! |

Each account starts with 1000 VLX. The `crash` game and the system settings come from the migration itself (`HasData`).

## Database

There are 30 tables, created by the migrations `InitialCreate` and `AddExtendedModels` (later migrations add columns):

| Area | Tables |
|---|---|
| Core (spec §6) | `Users, RefreshTokens, Wallets, WalletTransactions, Games, GameRounds, Bets, PlayerLimits, Notifications, AuditLogs, SystemSettings` |
| Security | `PasswordResetCodes, EmailVerificationCodes, UserTwoFactors, LoginAttempts` |
| Responsible play | `PlaySessions, SelfExclusions` |
| Profile and progress | `UserPreferences, PlayerStats, Levels, Achievements, UserAchievements` |
| Game engine | `AutoBetConfigs, GameRoundEvents, RoundCancellations` |
| Admin and content | `BalanceAdjustments, ChatMessages, ChatMutes, DailyStatistics, StaticPages` |

Seeded reference data: the `crash` game, system settings, 10 levels, 5 achievements, and 2 content pages.

Integrity is enforced in the database itself, not only in C#:

- CHECK constraints: balance ≥ 0, `BalanceAfter = BalanceBefore + Amount`, bet amount > 0, slot ∈ {1,2}, crash point ≥ 1, and more
- Unique indexes: email, nickname, game slug, wallet per user, transaction idempotency key, `(RoundId, UserId, Slot)` for bets
- `RowVersion` on `Wallets` for optimistic concurrency

## Crash game engine

A `BackgroundService` (`CrashGameEngine`) runs one loop per Crash game. The browser never drives the game.

```
Waiting → BettingOpen (10 s) → BettingClosed (1 s) → Running (100 ms ticks) → Crashed → Finished → pause (5 s)
                    any unfinished state ──────────────────────────────────→ Cancelled (all active bets refunded)
```

- **Provably fair:** before the round, the server picks a random 32-byte `serverSeed` and publishes `SHA-256(serverSeed)`. The crash point is `max(1, floor(100 · 0.97 / (1 − r)) / 100)`, where `r` is the first 52 bits of `HMAC-SHA256(serverSeed, "VELTRIX")` divided by 2^52. After the crash, the seed is revealed. Check any round with `GET /api/fairness/rounds/{id}` or `POST /api/fairness/verify`.
- **Multiplier:** `m(t) = e^(0.00006·ms)`, rounded down to 2 decimals. Cash-out uses the server's clock, never a value from the client.
- **Wallet:** every change is a `WalletTransaction` (before/after balance, unique idempotency key). `RowVersion` on wallets and `Status` on bets detect concurrent changes; the operation is then retried with fresh data.
- **Restart safety:** on startup, unfinished rounds are cancelled and their bets refunded.

## Real-time (SignalR) — `/hubs/crash`

Connect as a guest, or with `?access_token=<JWT>` to also receive your balance.

| Direction | Message | Payload |
|---|---|---|
| client → server | `JoinGame(slug)`, `LeaveGame(slug)` | |
| server → group | `RoundStateChanged` | roundId, roundNumber, state, serverSeedHash, bettingEndsAt, startedAt |
| server → group | `MultiplierTick` | roundId, multiplier, elapsedMs |
| server → group | `RoundCrashed` | roundId, crashPoint, serverSeed |
| server → group | `BetPlaced` / `PlayerCashedOut` | roundId, nickname, amount / multiplier, payout |
| server → user | `BalanceUpdated` | balance |
| server → all | `OnlineCount` | count |

## Tests

```bash
dotnet test
```

There are 104 xUnit tests: crash-point generation and distribution, provably fair verification, multiplier curve, round state transitions (illegal ones rejected), wallet rules (no negative balance, refunds, daily bonus), bet and cash-out rules, refresh-token reuse rules, a guard that no role-protected endpoint is accidentally `[AllowAnonymous]`, and which auth endpoints are rate-limited.

```bash
cd Veltrix.Web
npx ng test --watch=false --browsers=ChromeHeadless
```

There are 32 Jasmine tests:

- `AuthStore`: login, a single refresh shared by concurrent requests, a failed refresh ends the session, logout
- `CrashStore`: SignalR events → state (new round, crash, auto cash-out, cancel/refund), countdown on the server clock, extrapolation limit
- Crash math: the browser reproduces 7 real server rounds (seed → SHA-256 hash → crash point), proving both implementations are identical
- App shell: sidebar / top bar / bottom nav, guest navigation, collapsible sidebar, theme toggle

## Development notes

- The password reset code is "emailed" to the API console and log file (`Veltrix.API/Logs/`) in development.
- Logs: console + daily rolling file via Serilog.
- Git flow: `master` ← `develop` ← `feature/*`.

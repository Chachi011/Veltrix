# VELTRIX

A real-time multiplayer web platform whose main module is a **Crash game**: a multiplier grows during a round, and players must cash out before it crashes.

> **Educational / demo project.** Only virtual **VLX points** exist. There are no deposits, purchases, withdrawals or real money. Points come only from the starting balance, the daily bonus and achievements.

## Tech stack

| Layer | Technologies |
|---|---|
| Backend | C#, ASP.NET Core Web API (.NET 8), EF Core 8 (Code First), SQL Server, SignalR, JWT + refresh tokens, BCrypt, FluentValidation, Serilog, Swagger |
| Frontend | Angular (standalone), TypeScript, SCSS, NgRx SignalStore, @microsoft/signalr, Chart.js, Canvas *(Phase 3)* |
| Tests | xUnit + Moq, Jasmine/Karma *(Phase 5)* |

## Architecture

```
Veltrix.API            → controllers, middleware, filters, auth/Swagger/CORS setup   (depends on Application + Infrastructure)
Veltrix.Application    → DTOs, service interfaces, validators, exceptions            (depends on Domain)
Veltrix.Infrastructure → EF Core DbContext, migrations, service implementations, JWT, BCrypt
Veltrix.Domain         → entities, enums, business rules; no external packages
```

The dependencies point inward. The Domain project knows nothing about the database or the web, so its rules can be unit-tested in isolation.

## Prerequisites

- .NET SDK 8+ and the **ASP.NET Core 8 runtime** (`winget install Microsoft.DotNet.AspNetCore.8`)
- SQL Server (local default instance, Windows authentication)
- EF Core tools: `dotnet tool install --global dotnet-ef`

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

## Test accounts (Development seed)

| Role | Email | Password |
|---|---|---|
| Admin | admin@veltrix.dev | Admin123! |
| Operator | operator@veltrix.dev | Operator123! |
| Player | player1@veltrix.dev | Player123! |
| Player | player2@veltrix.dev | Player123! |

Each account starts with 1000 VLX. The `crash` game and the system settings come from the migration itself (`HasData`).

## Database

There are 30 tables, created by two migrations (`InitialCreate`, then `AddExtendedModels`):

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

## Development notes

- The password reset code is "emailed" to the API console and log file (`Veltrix.API/Logs/`) in development.
- Logs: console + daily rolling file via Serilog.
- Git flow: `master` ← `develop` ← `feature/*`.

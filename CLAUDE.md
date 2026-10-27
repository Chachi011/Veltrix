# VELTRIX: project rules

Diploma project: a real-time multiplayer web platform whose main module is a **Crash game** (a multiplier grows; players cash out before it crashes). The author must **defend it at an exam**, so every important decision is explained in simple terms.

**Educational / demo only: VIRTUAL points (VLX).** No deposits, purchases, withdrawals or real money, ever. Points come only from the starting balance, the daily bonus and achievements.

Source of truth for roles, endpoints and data models: the specification `VELTRIX-პროექტის-აღწერა.pdf` (on the author's Desktop).

## Always

- **Always follow [docs/DESIGN.md](docs/DESIGN.md) for any UI work.**
- **Whenever a design decision changes, update docs/DESIGN.md in the same commit** (and add a line to its decision log).
- Work **one phase at a time**. At the start of a phase list the files to create/change, then write them. After a phase: how to run and test it, then wait for the author's "next".
- Complete, compilable files: no `...` placeholders.
- Explain WHY for important design decisions, in simple language (exam defense).
- Verify for real before saying something works: build, run the tests, and exercise the running app (API + browser).

## Mandatory stack (graded, do not substitute)

| | |
|---|---|
| Backend | C#, ASP.NET Core Web API **.NET 8** (`net8.0`), SQL Server, EF Core 8 (Code First, migrations, seed), SignalR, JWT + refresh tokens, BCrypt, FluentValidation, Serilog, Swagger, xUnit + Moq |
| Frontend | Angular 20 (standalone, **zoneless**, signals), TypeScript, NgRx SignalStore (`@ngrx/signals`), Angular Router (role guards, lazy loading), HttpClient interceptors, Reactive Forms, `@microsoft/signalr`, Chart.js (Phase 4), HTML Canvas |
| UI | Tailwind CSS v4 + Angular CDK + lucide-angular, Inter / JetBrains Mono (details in docs/DESIGN.md) |
| Frontend tests | Jasmine / Karma |

The machine has the .NET 10 SDK plus the .NET 8 and ASP.NET Core 8 **runtimes** (8.0.32). Keep the projects on `net8.0`.

## Architecture

```
Veltrix.API            controllers, middleware (ProblemDetails), filters, SignalR hub, auth/Swagger/CORS/rate limiting setup
Veltrix.Application    DTOs, service interfaces, FluentValidation validators, app exceptions   (→ Domain)
Veltrix.Infrastructure EF Core DbContext + configurations + migrations, service implementations,
                       JWT/BCrypt, CrashGameEngine (BackgroundService), RoundSettlementService (→ Application, Domain)
Veltrix.Domain         entities with their business rules, enums, ProvablyFair, MultiplierCurve (no packages)
Veltrix.Tests          xUnit tests
Veltrix.Web            Angular client: core/ (api, auth, http, realtime, state, ui), features/, layout/, shared/ui/
```

- Dependencies point inward. The Domain knows nothing about the DB or the web, so rules are unit-tested without them.
- The Application layer must not reference ASP.NET Core types (e.g. `IFormFile` and `[BindNever]` stay in the API layer).
- Errors: throw `AppException` subclasses (Application) or `DomainException` (Domain); `ExceptionHandlingMiddleware` turns them into ProblemDetails. Controllers contain no try/catch.
- One `User` table with a `Role` field (Player, Operator, Admin). A guest is a request without a JWT.
- 30 tables (11 core from the spec + 19 supporting); see README → Database.

## Core game rules (never break)

- **The server owns the game.** `CrashGameEngine` (a BackgroundService) runs every round: `Waiting → BettingOpen → BettingClosed → Running → Crashed → Finished` (or `Cancelled` from any unfinished state). Illegal transitions throw (`GameRound` state machine).
- **The crash point is fixed BEFORE the round** (`GameRound.Create`): a random 32-byte server seed; its SHA-256 hash is published before betting; the seed is revealed after the crash. Crash point = `max(1, floor(100·0.97/(1−r))/100)`, where r is the first 52 bits of `HMAC-SHA256(seed, "VELTRIX")` / 2^52. The browser reproduces it (`crash-math.ts`); keep both implementations identical.
- **Bets only in BettingOpen; cash-out only while Running, at the SERVER's multiplier** (`m = e^(0.00006·ms)` on the server clock). The client sends requests, never results or multipliers.
- **Every balance change** = one DB transaction + a `WalletTransaction` row (balance before/after, unique idempotency key). `Wallet.RowVersion` and `Bet.Status` are concurrency tokens; conflicts are retried (`ConcurrencyRetry`). The balance can never go negative (domain check + DB CHECK constraint).
- A **cancelled round refunds every active bet** automatically (cashed-out bets keep their payout). On startup, unfinished rounds are cancelled and refunded.
- Seed and crash point are never shown before a round is over (`GameplayMapping.ToDto` is the one place that decides).

## Security rules learned the hard way

- `[AllowAnonymous]` on a controller **overrides** `[Authorize]` on its actions. Put it on individual actions; `AuthorizationAttributeTests` guards this.
- Refresh tokens rotate. A token rotated less than 30 s ago may be reused once (concurrent tabs/reloads); a ROTATED token replayed later revokes all sessions (theft). A token revoked for another reason (logout, password change, block) is just dead: 401, or 403 if the user is blocked, with no theft alarm (`RefreshToken.ReplayLooksLikeTheft`). The client serializes refreshes across tabs with Web Locks.
- Every `api/admin/*` controller has `[Authorize(Roles = Admin)]` on the CLASS (so new actions are protected automatically); `AuthorizationAttributeTests` guards this. Limits are Player-only, like bets.
- Admins cannot change their own role, block themselves or adjust their own balance (Domain rules).
- The `auth` rate limit (10/min/IP) covers login/register/forgot/reset only, not refresh/logout (`RateLimitAttributeTests`).
- Secrets (JWT key) live in user-secrets, never in Git.

## Phase plan

| Phase | Content | Status |
|---|---|---|
| 1 | Solution, DB, EF models + migrations, JWT auth, roles, Swagger, profile endpoints | **Done** (merged into `develop`) |
| 2 | Round engine, bets, cash-out, wallet, SignalR, provably fair (**MVP gate**) | **Done** (merged into `develop`) |
| 3 | Angular UI: design system, login/register, Crash page (Canvas), wallet, history, profile, responsive, dark/light | **Done** (verified by the author on desktop and phone; merged into `develop`) |
| 4 | Operator/Admin panels, leaderboard, limits/self-exclusion UI, notifications, settings, charts | **Done** (checked by the author; merged into `develop`) |
| 5 | Remaining tests (Moq service tests, more Jasmine), optional (*) features, README screenshots, seed script, presentation demo | Not started |

Optional (*) features only after the MVP: auto-bet, two bets per round (backend supports slot 2), achievements/XP, chat with moderation, 2FA, email verification, ka/en, CSV export, Docker Compose, GitHub Actions.

## Commands

```bash
dotnet run --project Veltrix.API --launch-profile http     # API on http://localhost:5295 (Swagger: /swagger); dev: auto-migrate + seed
dotnet test                                                # backend tests
cd Veltrix.Web && npm start                                # http://localhost:4200, proxies /api /hubs /uploads to :5295
cd Veltrix.Web && npx ng test --watch=false --browsers=ChromeHeadless
cd Veltrix.Web && npx ng build                             # production build: must have no warnings
dotnet ef migrations add <Name> --project Veltrix.Infrastructure --startup-project Veltrix.API --output-dir Persistence/Migrations
dotnet ef migrations has-pending-model-changes --project Veltrix.Infrastructure --startup-project Veltrix.API
```

Test accounts (development seed): `admin@veltrix.dev / Admin123!`, `operator@veltrix.dev / Operator123!`, `player1@veltrix.dev`, `player2@veltrix.dev / Player123!`. The login page has quick-fill buttons in development.

Stop the running API before `dotnet build` (it locks the DLLs). Check that port 4200 is free before `ng serve`.

## Conventions

- **Git:** `master` ← `develop` ← `feature/<phase-or-topic>`. Small, logical commits with clear messages. Merge features into `develop` with `--no-ff`. Do not push unless asked.
- **Line endings:** `.gitattributes` (CRLF for `.cs/.csproj/.slnx`, LF for `.ts/.html/.scss/.json`). Seed text in `HasData` is normalized with `ReplaceLineEndings("\n")`.
- **EF:** after model changes run `has-pending-model-changes`; never edit an applied migration (add a new one). Data fixes go in the migration (`migrationBuilder.Sql`).
- **Query DTOs:** computed values are methods, not properties (properties become query parameters; see `PageQuery.Offset()`).
- **Angular:** standalone components, OnPush, signals; stores are NgRx SignalStores. Components that show reactive-form errors use Default change detection. Interceptor order: loading → error → auth.
- **Real-time:** ONE SignalR connection per tab (`CrashHub`): the user session (balance, notifications) and a game page share it. Don't open a second connection (the online counter would count the tab twice).
- **Bundle:** `ng build` must stay under the 500 kB initial budget. Heavy libraries (Chart.js, SignalR) load with dynamic `import()`; eager code (layout, core) imports single `shared/ui` files, not the barrel.
- **Shell + backticks:** never write files containing backticks through bash `node -e` / heredocs (the shell eats them and has corrupted files); use the Write/Edit tools.
- **Tests:** every bug fix gets a test that fails without the fix.

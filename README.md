# VELTRIX

A real-time multiplayer web platform whose main module is a **Crash game**: a multiplier grows during a round, and players must cash out before it crashes.

> **Educational / demo project.** Only virtual **VLX points** exist. There are no deposits, purchases, withdrawals or real money. Points come only from the starting balance, the daily bonus and achievements.

## Tech stack

| Layer | Technologies |
|---|---|
| Backend | C#, ASP.NET Core Web API (.NET 8), EF Core 8 (Code First), SQL Server, SignalR, JWT + refresh tokens, BCrypt, FluentValidation, Serilog, Swagger |
| Frontend | Angular 20 (standalone, zoneless, signals), TypeScript, NgRx SignalStore, @microsoft/signalr, Chart.js, HTML Canvas, Tailwind CSS v4, Angular CDK |
| Tests | xUnit + Moq + WebApplicationFactory integration tests (backend), Jasmine/Karma (frontend) |

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
           http/ (error + loading interceptors), realtime/ (CrashHub: one shared SignalR connection),
           state/ (WalletStore, NotificationsStore, LimitsStore), ui/ (toasts, theme, layout)
features/  auth, crash (CrashStore, Canvas chart, bet panel, live bets, history strip), wallet, history, profile (+ statistics),
           fairness, leaderboard, limits (responsible play, session reminder), operator, admin
layout/    sidebar, top bar, bottom nav, notifications panel
shared/    ui/ (design system components), pipes, validators, formatters
```

## Design system

The UI follows [docs/DESIGN.md](docs/DESIGN.md): tokens, fonts, layout, components, motion and accessibility rules. In development, `/styleguide` shows every component in both themes.

## Specification coverage

[docs/SPEC-COVERAGE.md](docs/SPEC-COVERAGE.md) checks every item of the specification (sections 1–8: features 4.1–4.11, API endpoints, data models, quality and delivery) against the code: done / partial / missing, with the file that implements it. Optional (\*) items are listed separately.

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

**API in Postman:** File → Import → choose [`docs/api/openapi.json`](docs/api/openapi.json) (or the live URL `http://localhost:5295/swagger/v1/swagger.json`). Postman creates a request for every endpoint; set the collection's Bearer token to the `accessToken` from `POST /api/auth/login`.

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

### Demo data (Development only)

So that the leaderboard, the charts and the admin dashboard are not empty in a demo, the first Development start also creates **15 demo players with 30 days of play** (about 230 rounds and 1300 bets; it takes about half a minute, once). They log in with `<nickname>@demo.veltrix.dev` / `Player123!`, e.g. `nova@demo.veltrix.dev`.

The demo history is played by the same code as a real game (`DemoDataSeeder`), only with a clock set to moments in the past:

- every round has a real random seed, hash and crash point, so "verify this round" works for it;
- every bet passes `Bet.Place` and takes the stake from the wallet with a ledger row; a player who runs out of points comes back only through the daily bonus;
- cash-outs happen below the crash point (auto cash-out through the engine's settlement code, or "by hand" at the server multiplier of that moment);
- the crash settles the remaining bets as lost and updates the player statistics; the admin daily statistics are recalculated for the days that got demo bets.

It runs only when `Database:SeedDemoData` is `true` (set in `appsettings.Development.json`) and is skipped once the demo players exist. An integration test (`DemoDataSeederTests`) checks these rules on SQL Server.

## Database

There are 30 tables, created by the migrations `InitialCreate` and `AddExtendedModels` (later migrations add columns, constraints and indexes):

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

## Player tools and admin panel (Phase 4)

| Area | Endpoints | Who |
|---|---|---|
| Leaderboard | `GET /api/leaderboard?period=day\|week\|all&type=multiplier\|profit&limit=20` | everyone (guests too) |
| Personal stats | `GET /api/stats/me?days=30` | logged in |
| Limits | `GET /api/limits`, `PUT /api/limits`, `POST /api/limits/self-exclusion` | Player |
| Notifications | `GET /api/notifications`, `PUT /api/notifications/{id}/read`, `PUT /api/notifications/read-all` | logged in |
| Admin users | `GET /api/admin/users?search=&role=&isBlocked=&page=`, `GET /api/admin/users/{id}`, `PUT …/{id}/role`, `PUT …/{id}/block`, `POST …/{id}/balance-adjustment` | Admin |
| Audit log | `GET /api/admin/audit-logs?action=&entityType=&entityId=&page=`, `GET /api/admin/audit-logs/actions` | Admin |
| Settings | `GET /api/admin/settings`, `PUT /api/admin/settings` (starting balance, daily bonus) | Admin |
| Dashboard | `GET /api/admin/stats/overview`, `GET /api/admin/stats/daily?from=&to=` | Admin |

Rules (all in Domain entities, all unit-tested):

- **Daily bet limit** (`PlayerLimit`): a lower limit applies at once; a higher limit or removing it waits **24 hours**. Otherwise a player on a losing streak could just raise it. The bet service checks it inside the same retried unit of work as the wallet debit, so two simultaneous bets cannot both slip under the limit.
- **Self-exclusion** (24 h / 7 d / 30 d): betting is refused with 403. It can be extended but never shortened or cancelled. The player can still sign in and see their history.
- **Balance adjustment** (`BalanceAdjustment.Apply`): a reason (5–500 characters) is required, the amount is never 0, the balance never goes negative, and an admin cannot adjust their own balance. It creates an `AdminAdjustment` ledger row, an audit entry and a notification in one transaction. An optional `requestId` makes it idempotent (a double-click cannot pay twice).
- **Role and block** (`User`): nobody can change their own role or block themselves, so the system can never lose its last admin. Blocking signs the user out on every device and refuses their bets at once.
- **Audit log:** every admin action (and limit changes and self-exclusions) records who did it, the old value and the new value.

Background jobs (`Infrastructure/Jobs`):

- `DailyBonusReminderJob` (every minute): creates a "Daily bonus available" notification once per bonus cycle and pushes it live. It skips blocked and self-excluded players.
- `DailyStatisticsJob` (startup, then every 10 minutes): stores one `DailyStatistics` row per finished day, so the admin charts read about 30 small rows instead of scanning every bet. Today is always calculated live.

The admin dashboard shows the crash points of the last 1000 rounds next to the share the formula predicts (`P(crash ≥ x) = 0.97 / x`). Over many rounds the two must match. A unit test checks this on 20,000 generated rounds.

Pages (Angular):

| Page | Who | What |
|---|---|---|
| `/` (Home) | everyone | the live round, available games, top players of the day |
| `/games` | everyone | games catalog: search, type and status filters; `/crash/:slug` opens any Crash game |
| `/players/:nickname` | everyone | public profile: level, totals, best cash-outs |
| `/rules` | everyone | game rules and "why there is no winning strategy" (from the `StaticPages` table, `GET /api/pages/{slug}`) |
| `/settings` | logged in | theme (System / Dark / Light), game sounds, animations; saved on the account (`GET/PUT /api/profile/preferences`) |
| `/leaderboard` | everyone | 24 hours / 7 days / all time × best multiplier / profit; podium + table, my rank highlighted |
| `/profile` → Statistics | players | win rate, average cash-out, net result, charts (results per day, win rate, cash-out ranges) |
| `/limits` (Responsible play) | players | daily limit with today's usage, session reminder, self-exclusion with a confirm dialog |
| bell (top bar) | logged in | unread badge, latest notifications, mark read; new ones arrive live |
| `/operator` | Operator, Admin | live round monitor, cancel round (reason, automatic refunds), games and their status |
| `/admin`, `/admin/users`, `/admin/audit`, `/admin/settings` | Admin | dashboard charts, users + drawer (role, block, balance adjustment), audit log, settings |

The browser keeps **one** SignalR connection per tab: the logged-in user receives balance and notifications on every page, and the Crash page (or the operator monitor) joins its game group on the same connection, so the online counter counts each tab once. Chart.js and the SignalR client are loaded on demand, so the first download stays under 500 kB.

The **session reminder** (every 60 minutes by default) runs in the browser: it is advice. The hard rules (daily limit, self-exclusion) are enforced by the server on every bet.

## Real-time (SignalR) — `/hubs/crash`

Connect as a guest, or with `?access_token=<JWT>` to also receive your balance and notifications.

| Direction | Message | Payload |
|---|---|---|
| client → server | `JoinGame(slug)`, `LeaveGame(slug)` | |
| server → group | `RoundStateChanged` | roundId, roundNumber, state, serverSeedHash, bettingEndsAt, startedAt |
| server → group | `MultiplierTick` | roundId, multiplier, elapsedMs |
| server → group | `RoundCrashed` | roundId, crashPoint, serverSeed |
| server → group | `BetPlaced` / `PlayerCashedOut` | roundId, nickname, amount / multiplier, payout |
| server → user | `BalanceUpdated` | balance |
| server → user | `Notification` | id, type, title, message, isRead, createdAt |
| server → all | `OnlineCount` | count |

## Tests

```bash
dotnet test
```

There are 246 xUnit tests in three groups:

- **Integration tests** (`Veltrix.Tests/Integration`, 28 tests): `WebApplicationFactory` starts the real API in memory (same `Program.cs`, middleware, JWT, validation, rate limits, EF Core) against a **throwaway SQL Server database** `Veltrix_Tests_<random>`, created by the migrations and dropped at the end, and calls it over HTTP. The crash engine's background loop is off, so each test moves its own round through the states with `RoundSettlementService` (the engine's own code) and a clock it can stop. Covered: register / login / validation errors per field / duplicate email; 401 without or with a forged token; refresh-token rotation, logout, the 30-second reuse window and the theft alarm (an old token replayed later logs out every session); the auth rate limit (11th login from one IP → 429, refresh not limited); a bet takes the stake and writes a ledger row; bets refused after betting closes, below the minimum, with 3 decimals, twice in one slot, or above the balance; cash-out pays the stake × the **server's** multiplier at the moment of the request; too late after the crash point; the crash settles the bet as lost and reveals the crash point; an operator's cancel refunds active bets while cash-outs keep their payout; only players can bet; a blocked player is refused even with a token issued before the block; a self-excluded player cannot bet; every `api/admin` endpoint answers 401 / 403 / 403 / 200 to guest / player / operator / admin; only staff can cancel a round; an admin cannot change their own role, block themselves or adjust their own balance; a balance adjustment writes a ledger row and an audit entry; a promoted player gets the new role at the next login; the demo data follows the game rules (ledger chain, no negative balance, cash-outs below the crash point, verifiable crash points, statistics, created only once). They need a SQL Server: `localhost` with Windows login by default, or set `VELTRIX_TEST_SQL` to another connection string (without a database name).
- **Service tests with Moq** (`Veltrix.Tests/Services`) run the real services against EF Core's in-memory database, with Moq fakes for "who is calling" and the SignalR notifier, and check both the saved data and the live events sent (wallet, limits, admin actions, preferences, content pages, the daily-bonus job).
- **Domain and API tests** cover crash-point generation and distribution, provably fair verification, multiplier curve, round state transitions (illegal ones rejected), wallet rules (no negative balance, refunds, daily bonus), bet and cash-out rules, refresh-token reuse rules, daily bet limit (24-hour wait before an increase), self-exclusion, admin balance adjustment, role and block rules, crash-point ranges against the theoretical distribution, guards that no role-protected endpoint is accidentally `[AllowAnonymous]` and that every `api/admin` controller requires the Admin role, and which auth endpoints are rate-limited.

```bash
cd Veltrix.Web
npx ng test --watch=false --browsers=ChromeHeadless
```

There are 112 Jasmine tests:

- `AuthStore`: login, a single refresh shared by concurrent requests, a failed refresh ends the session, logout
- `WalletStore`, `LimitsStore`: balance, daily bonus (success / refused), filters, self-exclusion, refused updates
- `PreferencesStore`: animations off = reduced motion, guests keep settings locally, logged-in players save them, the account's settings win after login
- `ToastStore`, `ThemeService`: max 4 toasts, errors stay longer; System / Dark / Light
- HTTP interceptors: ProblemDetails message as a toast, quiet for 401 and silent requests, loading bar counting
- Auth interceptor: the access token goes to API calls only (not to /api/auth or other files), a 401 triggers ONE refresh and a retry with the new token, a failed refresh ends the session, no refresh for guests or for 403
- ProblemDetails helpers: any error becomes one toast sentence, server validation errors appear next to the matching form fields
- Form validators (same rules as the server): nickname, strong password, 18+, password confirmation, 2 decimals
- `SoundService`, `LayoutService`: silent while sounds are off, one audio context; the collapsed sidebar is remembered
- Markdown (rules pages): lists, bold / italic, HTML stays text, no extra spaces
- `CrashStore`: SignalR events → state (new round, crash, auto cash-out, cancel/refund), countdown on the server clock, extrapolation limit, game sounds (once per result)
- `CrashHub`: one shared SignalR connection for the user and the game page, the online count handed to a page that joins later, leave / logout, a new connection for a new identity, personal vs game events
- `NotificationsStore`: load, live notifications (deduplicated), optimistic mark read / mark all read, a role change refreshes the token and leaves a forbidden page, a block signs the user out
- `roleGuard`: the right roles get in, others go back to the game, guests go to the login page
- Bet panel: the one big button (`betAction`), including "betting paused" for a self-excluded player
- Session reminder (`play-session`): continuous play, breaks, reminders every N minutes
- Audit log text: "Role: Player → Operator", readable dates
- Crash math: the browser reproduces 7 real server rounds (seed → SHA-256 hash → crash point), proving both implementations are identical
- App shell: sidebar / top bar / bottom nav, guest navigation, collapsible sidebar, theme toggle

## Development notes

- The password reset code is "emailed" to the API console and log file (`Veltrix.API/Logs/`) in development.
- Logs: console + daily rolling file via Serilog.
- Git flow: `master` ← `develop` ← `feature/*`.

# VELTRIX: specification coverage

Audit of the project against the specification **VELTRIX-პროექტის-აღწერა.pdf** (14 pages, Oct 23, 2026), a copy of which is kept in this repository as [`docs/SPEC.pdf`](SPEC.pdf), section by section.

- Audited on **2026-10-27** as **Phase 5 · Step 1**, branch `feature/phase5` (Phases 1–4 done and merged into `develop`). Updated at the end of every Phase 5 step; last update: **Step 3 (optional features)**, see [§ Phase 5 Step 3](#phase-5-step-3-optional-features).
- **Status:** ✅ done · 🟡 partial · ❌ missing.
- Items marked **(\*)** in the spec are optional ("after the main part"); they are listed separately in [§ Optional (\*) items](#optional--items).
- **Added in this pass** = the gap was found by this audit and closed in Step 1 (see [§ Gaps closed in this pass](#gaps-closed-in-this-pass)).
- Next steps that will change this file: Step 3 (the optional items in the author's order), Step 4 (README screenshots, `/database` script, presentation).

## Summary

| | ✅ Done | 🟡 Partial | ❌ Missing |
|---|---|---|---|
| Required (non-\*) items | 147 | 5 | 0 |
| Optional (\*) items | 3 | 2 | 13 |

The 5 partial required items are all decisions or Phase 5 delivery work, not missing features: README screenshots, the standalone seed SQL script and the presentation (Phase 5 by plan), the `master` branch name (the spec says `main`) and plain CSS via Tailwind v4 instead of SCSS. Details in [§ Still open](#still-open-non-).

---

## 1. Project goal and principles

| Item | Status | Where in the code |
|---|---|---|
| Real-time multiplayer web platform, main module Crash | ✅ | `Veltrix.Infrastructure/Engine/CrashGameEngine.cs`, `Veltrix.Web/src/app/features/crash` |
| Virtual VLX points only, no monetary value | ✅ | `TransactionType` has no Deposit/Withdrawal (`Veltrix.Domain/Enums/TransactionType.cs`); disclaimers in sidebar, wallet, rules |
| Buying, deposit, withdrawal or exchange technically impossible | ✅ | No such endpoint, entity or transaction type exists |
| Points only from starting balance, daily bonus (and achievements \*) | ✅ | `User.Create` (welcome Bonus), `Wallet.ClaimDailyBonus`; admin corrections are audited `AdminAdjustment`s |
| Self-control tools: limits, session reminder, temporary suspension | ✅ | §4.8 below |
| Works on desktop and mobile; secure data handling | ✅ | Responsive layout (docs/DESIGN.md §4), §7 security below |

## 2. Technical requirements

### Back-end

| Item | Status | Where in the code |
|---|---|---|
| C# | ✅ | All `Veltrix.*` projects |
| ASP.NET Core Web API (.NET 8) | ✅ | `Veltrix.API` (`net8.0`) |
| Microsoft SQL Server | ✅ | `UseSqlServer` in `Veltrix.Infrastructure/DependencyInjection.cs` |
| EF Core: Code First, migrations, seed | ✅ | `Persistence/Configurations/*`, `Persistence/Migrations/*` (4 migrations), `HasData` seeds + `DatabaseInitializer` (test users) + `DemoDataSeeder` (Development demo history) |
| SignalR real time | ✅ | `Veltrix.API/Hubs/CrashHub.cs`, `Services/SignalRGameNotifier.cs` |
| JWT access + refresh token, role-based access | ✅ | `Infrastructure/Auth/JwtTokenService.cs`, `AuthService`, `RefreshToken` rotation |
| BCrypt password hashing | ✅ | `Infrastructure/Auth/BCryptPasswordHasher.cs` |
| FluentValidation | ✅ | `Veltrix.Application/Validators/*`, `API/Filters/FluentValidationFilter.cs` |
| Game engine as BackgroundService (IHostedService) | ✅ | `CrashGameEngine : BackgroundService` |
| Serilog (console + file) | ✅ | `Program.cs` (`UseSerilog`), `appsettings.json` → `Serilog.WriteTo` Console + File |
| Swagger / OpenAPI | ✅ | `API/Extensions/ApiServiceExtensions.cs` (`AddSwaggerWithJwt`), XML comments on every action |
| Testing: xUnit + Moq | ✅ | `Veltrix.Tests` (246 tests); Moq service tests in `Veltrix.Tests/Services/*` — **Moq added in this pass**; WebApplicationFactory integration tests in `Veltrix.Tests/Integration/*` (Step 2) |
| Layered architecture (API → Application → Domain → Infrastructure) | ✅ | 4 projects + tests; dependencies point inward (CLAUDE.md → Architecture) |

### Front-end

| Item | Status | Where in the code |
|---|---|---|
| Angular, standalone components | ✅ | Angular 20, every component `standalone` (default), zoneless |
| TypeScript, HTML | ✅ | `Veltrix.Web/src/app` |
| SCSS | 🟡 | The author chose **Tailwind CSS v4**, which is configured in plain CSS and does not support Sass (docs/DESIGN.md §1). Component styles may use SCSS; none needs it |
| State: Signals + stores with NgRx SignalStore | ✅ | `AuthStore`, `WalletStore`, `NotificationsStore`, `LimitsStore`, `PreferencesStore`, `ToastStore`, `LoadingStore` (`core/`), `CrashStore` (`features/crash`) |
| Router: role guards, lazy loading | ✅ | `app.routes.ts` (every page `loadComponent`), `core/auth/auth.guards.ts` (`authGuard`, `guestGuard`, `roleGuard`) |
| HttpClient interceptors (JWT, errors, loading) | ✅ | `core/auth/auth.interceptor.ts`, `core/http/error.interceptor.ts`, `core/http/loading.interceptor.ts` |
| @microsoft/signalr client | ✅ | `core/realtime/crash-hub.ts` (one shared connection) |
| Reactive Forms with validation | ✅ | Auth, profile, limits, bet panel, game form, admin drawer, settings forms; `shared/validators.ts` |
| Chart.js (statistics, multiplier curve) | ✅ | `shared/ui/chart.ts` (`vx-chart`, lazy-loaded) for statistics and dashboards; the live multiplier curve uses HTML Canvas directly (`crash-chart.ts`), which the spec lists as the animation technology |
| HTML Canvas for the multiplier | ✅ | `features/crash/components/crash-chart.ts` |
| Jasmine / Karma (services and stores) | ✅ | 112 tests, `*.spec.ts` — **more store/service tests added in this pass** and in Step 2 |

### Tools

| Item | Status | Where |
|---|---|---|
| Git + GitHub, branches per feature | ✅ | `master` ← `develop` ← `feature/*`, remote `origin` |
| Postman for API checking | ✅ | `docs/api/openapi.json` imports straight into Postman (README → API in Postman) — **added in this pass** |
| Docker Compose (\*) | ❌ | see optional items |
| GitHub Actions (\*) | ❌ | see optional items |

## 3. User roles

| Item | Status | Where in the code |
|---|---|---|
| Four roles; all but Guest in ONE `User` table with a `Role` field | ✅ | `Domain/Entities/User.cs`, `Domain/Enums/UserRole.cs`; guest = request without JWT |
| Guest: home page | ✅ | `/` → `features/home/home-page.ts` — **added in this pass** |
| Guest: games catalog | ✅ | `/games` → `features/games/games-page.ts` — **added in this pass** |
| Guest: watch the live round | ✅ | `/crash` is public; `CrashHub` accepts anonymous connections |
| Guest: leaderboard | ✅ | `/leaderboard`, `GET /api/leaderboard` `[AllowAnonymous]` |
| Guest: rules | ✅ | `/rules` → `features/rules/rules-page.ts`, `GET /api/pages/{slug}` — **added in this pass** |
| Player: bets, cash-out, wallet, history, statistics, profile | ✅ | `BetsController` (Player only), `/wallet`, `/history`, `/profile` (statistics), `/limits` |
| Player: chat (\*), achievements (\*) | ❌ | see optional items |
| Operator: games and parameters, round monitoring and cancellation | ✅ | `/operator` (`features/operator`), `GamesController` / `RoundsController.Cancel` `[Authorize(Roles = Operator,Admin)]` |
| Operator: chat moderation (\*) | ❌ | see optional items |
| Admin: users and roles, system statistics, audit log, settings | ✅ | `/admin/*` (`features/admin`), `Admin*Controller` with `[Authorize(Roles = Admin)]` on the class |
| Access checked on the server (`[Authorize(Roles)]`) AND by route guards; the server decides | ✅ | `AuthorizationAttributeTests`, `auth.guards.spec.ts` |

## 4. Main functionality

### 4.1 Authorization

| Item | Status | Where in the code |
|---|---|---|
| Registration: email, nickname, password, 18+ confirmation | ✅ | `AuthController.Register`, `RegisterRequestValidator` (age ≥ 18, `IsAdultConfirmed`), `features/auth/register-page.ts` |
| Login with JWT; session continued with refresh token | ✅ | `AuthService.LoginAsync` / `RefreshAsync`, `core/auth/auth.store.ts` (refresh shared across tabs via Web Locks) |
| Logout and refresh-token revocation | ✅ | `AuthService.LogoutAsync`, `AuthStore.logout` |
| Password recovery with a one-time code | ✅ | `AuthService.ForgotPasswordAsync` / `ResetPasswordAsync`, `PasswordResetCode`, `features/auth/forgot-password-page.ts` (the code is "emailed" to the log in development) |
| Profile management: avatar, nickname, password change | ✅ | `ProfileController` (PUT, `/password`, `/avatar`), `features/profile` |
| Temporary lock after 5 failed attempts | ✅ | `User.RegisterFailedLogin` (`MaxFailedLoginAttempts = 5`, 15 min), `LoginAttempt` log |
| Email verification (\*), two-factor (\*) | ❌ | see optional items |

### 4.2 Crash game engine

| Item | Status | Where in the code |
|---|---|---|
| Round lifecycle Waiting → BettingOpen → BettingClosed → Running → Crashed → Finished (or Cancelled) | ✅ | `Domain/Entities/GameRound.cs` (illegal jumps throw), `GameRoundStateTests` |
| Rounds driven by the server's BackgroundService, not the browser | ✅ | `CrashGameEngine`, `RoundSettlementService` |
| Crash point generated on the server before the round | ✅ | `GameRound.Create` → `ProvablyFair.CalculateCrashPoint` |
| Multiplier grows exponentially with time | ✅ | `Domain/Crash/MultiplierCurve.cs` (`e^(0.00006·ms)`), same formula in `crash-math.ts` |
| Bets only in BettingOpen, within the game's min/max | ✅ | `Bet.Place`, `BetService.PlaceBetAsync`, `BetTests` |
| Cash-out only while Running, at the server's current multiplier | ✅ | `Bet.CashOut`, `BetService.CashOutAsync` (server clock) |
| Automatic cash-out at a preset multiplier | ✅ | `Bet.AutoCashOutAt`, `RoundSettlementService.AutoCashOutAsync`, bet panel "Auto cash-out" |
| All bets refunded when a round is cancelled | ✅ | `RoundSettlementService.CancelAsync` (+ on startup `RecoverUnfinishedRoundsAsync`) |
| The player only sends a request; the win is calculated on the server | ✅ | `POST /api/bets/{id}/cashout` has no body; result comes from the server |
| Two parallel bets (\*), auto-bet (\*) | 🟡 / ❌ | see optional items |

### 4.3 Provably fair

| Item | Status | Where in the code |
|---|---|---|
| A secret seed per round | ✅ | `ProvablyFair.GenerateServerSeed` (32 random bytes) |
| Its SHA-256 hash published before the round | ✅ | `RoundStateChangedEvent.ServerSeedHash`, hash badge on the Crash page |
| Seed and crash point published after the round | ✅ | `RoundCrashedEvent`, `GameplayMapping.ToDto` (the only place that reveals them) |
| Verification page anyone can use | ✅ | `FairnessController` (`GET /api/fairness/rounds/{id}`, `POST /api/fairness/verify`), `features/fairness` (+ in-browser recomputation in `crash-math.ts`) |

### 4.4 Virtual wallet

| Item | Status | Where in the code |
|---|---|---|
| Starting balance at registration (e.g. 1000 VLX) | ✅ | `User.Create`, setting `Wallet.StartingBalance` (admin-editable) |
| Daily bonus once per 24 hours | ✅ | `Wallet.ClaimDailyBonus`, `POST /api/wallet/daily-bonus`, top bar + wallet page |
| Transaction types Bet, Win, Refund, Bonus, AdminAdjustment | ✅ | `Domain/Enums/TransactionType.cs` |
| Balance before and after on every transaction | ✅ | `WalletTransaction.BalanceBefore/After` + CHECK `BalanceAfter = BalanceBefore + Amount` |
| Balance can never go negative (DB constraint + DB transaction) | ✅ | `Wallet.Debit`, CHECK constraints on `Wallets` and `WalletTransactions`, one SaveChanges per change |
| Protection against double execution (idempotency key, RowVersion) | ✅ | Unique index on `IdempotencyKey`, `Wallet.RowVersion`, `ConcurrencyRetry`, `WalletTests.DailyBonus_EachClaimHasADifferentIdempotencyKey`, admin `requestId` |
| Transaction history with filters and pages | ✅ | `GET /api/wallet/transactions?type=&from=&to=&page=`, wallet page (type filter + pagination) |
| Balance refill when it reaches zero (\*) | ❌ | see optional items |

### 4.5 Real time (SignalR)

| Item | Status | Where in the code |
|---|---|---|
| Multiplier updates to all viewers at once | ✅ | `MultiplierTick` every 100 ms to the game group |
| Live list of the current round's bets | ✅ | `BetPlaced` / `PlayerCashedOut` events, `features/crash/components/live-bets.ts` |
| Strip of the last 20 crash points | ✅ | `history-strip.ts` (`HISTORY_SIZE = 20`) |
| Online players count | ✅ | `CrashHub.OnlineCount`, `OnlineCount` event, Crash page / home page / operator monitor |
| Instant balance update after a cash-out | ✅ | `BalanceUpdated` to `Clients.User(userId)` |
| Automatic reconnect | ✅ | `withAutomaticReconnect` in `crash-hub.ts`, reconnect banner |
| Chat with moderation (\*) | ❌ | see optional items |

### 4.6 Leaderboard and achievements

| Item | Status | Where in the code |
|---|---|---|
| Leaderboard: day, week, all time | ✅ | `LeaderboardService` (day = last 24 h, week = last 7 days), `/leaderboard` |
| Ranking: biggest multiplier, biggest virtual win | ✅ | `type=multiplier` (best cash-out) and `type=profit` (net virtual win = payouts − stakes) |
| Public player profile (nickname, avatar, best results) | ✅ | `GET /api/players/{nickname}` + `/players/:nickname` (`features/players/player-page.ts`), nicknames link to it from the leaderboard, live bets and home — **page added in this pass** |
| Achievements / badges (\*), levels and XP (\*) | ❌ / 🟡 | see optional items |

### 4.7 Player interface

| Item | Status | Where in the code |
|---|---|---|
| Home page: available games, live round, short leaderboard | ✅ | `features/home` (`home-page`, `live-round-card`) — **added in this pass** |
| Games catalog with search and filters (type, status) | ✅ | `features/games` (search, type, status in the URL; Disabled only for staff), `GET /api/games?search=&type=&status=`, `/crash/:slug` per game — **page added in this pass** |
| Crash page: Canvas curve, bet form, cash-out button, auto cash-out field, live bets | ✅ | `features/crash` |
| History page: bets and rounds, filters by date and result, pagination | ✅ | `features/history` (My bets: status + date range; All rounds) |
| Personal statistics with charts: bet count, win share, average cash-out | ✅ | `features/profile/my-stats.ts`, `GET /api/stats/me` |
| Settings: dark/light theme, sound, animations off | ✅ | `/settings` (`features/settings`), `PreferencesStore`, `SoundService`, `GET/PUT /api/profile/preferences` (`UserPreference` table), `[data-motion='reduce']` in `styles.css` — **sound + animations + account sync added in this pass** (theme existed) |
| Georgian and English (\*) | ✅ | Georgian default + English, switch in Settings / account menu / top bar for guests; see [§ Phase 5 Step 3](#phase-5-step-3-optional-features) |

### 4.8 Self-control tools

| Item | Status | Where in the code |
|---|---|---|
| Daily limit on the total of virtual bets | ✅ | `PlayerLimit` (higher limit waits 24 h), `BetService` check, `/limits` |
| Reminder every 60 minutes of continuous play | ✅ | `features/limits/session-reminder.ts` + `play-session.ts` (interval from `PlayerLimit.SessionReminderMin`) |
| Temporary account suspension (24 h, 7 d, 30 d) | ✅ | `PlayerLimit.StartSelfExclusion`, `POST /api/limits/self-exclusion`, confirm dialog, "Betting paused" on the Crash page |
| Page explaining why Crash has no "winning strategy" (\*) | ✅ | see optional items |

### 4.9 Operator panel

| Item | Status | Where in the code |
|---|---|---|
| Create and edit games (name, slug, description, min/max bet) | ✅ | `POST/PUT /api/games`, `features/operator/game-form-dialog.ts` |
| Game status Active / Maintenance / Disabled | ✅ | `PATCH /api/games/{id}/status`, status select (confirmation for non-Active), Crash page notice |
| Round parameters: betting duration, pause between rounds | ✅ | `Game.BettingDurationSec` / `PauseBetweenRoundsSec` in the game form |
| Live monitoring of the current round | ✅ | Operator → Live round (same real-time store as the Crash page) |
| Round cancellation with automatic refunds | ✅ | `POST /api/rounds/{id}/cancel` (reason required, audited), `cancel-round-dialog.ts` |
| Chat moderation (\*) | ❌ | see optional items |

### 4.10 Admin panel

| Item | Status | Where in the code |
|---|---|---|
| Users list with search, filter and pagination | ✅ | `GET /api/admin/users?search=&role=&isBlocked=&page=`, `/admin/users` |
| Block / unblock users and change roles | ✅ | `PUT …/{id}/block` (signs out everywhere), `PUT …/{id}/role`, user drawer |
| Virtual balance correction with a mandatory reason | ✅ | `BalanceAdjustment.Apply`, `POST …/{id}/balance-adjustment`, drawer form with confirm |
| Dashboard with charts: active users, rounds per day, bet volume, crash-point distribution | ✅ | `GET /api/admin/stats/overview` + `/daily`, `DailyStatisticsJob`, `/admin` (crash points vs the formula's prediction) |
| Audit log: who changed what and when | ✅ | `AuditLog` written in the same transaction as each change, `/admin/audit` |
| System settings (starting balance, daily bonus amount) | ✅ | `GET/PUT /api/admin/settings`, `/admin/settings` |
| CSV export of reports (\*) | ❌ | see optional items |

### 4.11 Notifications

| Item | Status | Where in the code |
|---|---|---|
| System notifications on the bell (bonus available, new achievement, account change) | ✅ | `Notification` + `NotificationTypes`, `DailyBonusReminderJob`, admin/profile/limits services, live `Notification` SignalR event, `layout/notifications-panel.ts` ("new achievement" waits for the optional achievements) |
| Mark as read | ✅ | `PUT /api/notifications/{id}/read`, `/read-all`, `NotificationsStore` |
| Toasts on success and error | ✅ | `ToastStore`, `error.interceptor.ts` |
| Email notifications (\*) | ❌ | see optional items |

## 5. API endpoints

All documented in Swagger (`/swagger`); errors are ProblemDetails (`ExceptionHandlingMiddleware`). **51 operations** in total (the spec's evaluation table mentions 50).

| Group | Endpoints | Status | Controller |
|---|---|---|---|
| AUTH | `POST /api/auth/register`, `/login`, `/refresh`, `/logout`, `/forgot-password`, `/reset-password` | ✅ | `AuthController` |
| PROFILE | `GET/PUT /api/profile`, `PUT /api/profile/password`, `POST /api/profile/avatar`, `GET /api/players/{nickname}` | ✅ | `ProfileController`, `PlayersController` |
| GAMES | `GET /api/games`, `GET /api/games/{slug}`, `POST /api/games`, `PUT /api/games/{id}`, `PATCH /api/games/{id}/status` | ✅ | `GamesController` |
| ROUNDS | `GET /api/games/{slug}/rounds/current`, `GET /api/games/{slug}/rounds?page=&pageSize=`, `GET /api/rounds/{id}`, `GET /api/rounds/{id}/bets`, `POST /api/rounds/{id}/cancel` | ✅ | `RoundsController` (the page size parameter is `pageSize`, the same on every paged endpoint) |
| BETS | `POST /api/bets`, `POST /api/bets/{id}/cashout`, `GET /api/bets/my?from=&to=&status=&page=` | ✅ | `BetsController` |
| WALLET | `GET /api/wallet`, `GET /api/wallet/transactions?type=&page=`, `POST /api/wallet/daily-bonus` | ✅ | `WalletController` |
| FAIRNESS | `GET /api/fairness/rounds/{id}`, `POST /api/fairness/verify` | ✅ | `FairnessController` |
| LEADERBOARD | `GET /api/leaderboard?period=day\|week\|all&type=multiplier\|profit` | ✅ | `LeaderboardController` |
| STATISTICS | `GET /api/stats/me`, `GET /api/admin/stats/overview`, `GET /api/admin/stats/daily?from=&to=` | ✅ | `StatsController`, `AdminStatsController` |
| LIMITS | `GET /api/limits`, `PUT /api/limits`, `POST /api/limits/self-exclusion` | ✅ | `LimitsController` |
| NOTIFICATIONS | `GET /api/notifications`, `PUT /api/notifications/{id}/read`, `PUT /api/notifications/read-all` | ✅ | `NotificationsController` |
| ADMIN | `GET /api/admin/users`, `GET /api/admin/users/{id}`, `PUT …/role`, `PUT …/block`, `POST …/balance-adjustment`, `GET /api/admin/audit-logs`, `GET/PUT /api/admin/settings` | ✅ | `AdminUsersController`, `AdminAuditLogsController`, `AdminSettingsController` |
| ACHIEVEMENTS (\*) `GET /api/achievements`, `/my` · `GET /api/admin/reports/export` (\*) | | ❌ | see optional items |
| Extra (not in the spec, needed by spec features) | `GET/PUT /api/profile/preferences` (settings), `GET /api/pages/{slug}` (rules), `GET /api/admin/audit-logs/actions` | ✅ | `ProfileController`, `PagesController`, `AdminAuditLogsController` |

**SignalR hub `/hubs/crash`**

| Event | Status | Where |
|---|---|---|
| Server → client: `RoundStateChanged`, `MultiplierTick`, `RoundCrashed`, `BetPlaced`, `PlayerCashedOut`, `BalanceUpdated`, `OnlineCount` | ✅ | `SignalRGameNotifier`, `CrashHub` (+ `Notification` for the bell) |
| Client → server: `JoinGame(slug)`, `LeaveGame(slug)` | ✅ | `CrashHub` |
| `ChatMessage` / `SendChatMessage` (\*) | ❌ | see optional items |

## 6. Data models

| Model (spec fields) | Status | Where |
|---|---|---|
| User (id, email unique, nickname unique, passwordHash, role, avatarUrl, dateOfBirth, isBlocked, failedLoginCount, lockoutUntil, createdAt) | ✅ | `Domain/Entities/User.cs`, `UserConfiguration` (unique indexes) |
| RefreshToken (id, userId, tokenHash, expiresAt, revokedAt) | ✅ | `RefreshToken.cs` (+ `RevokedReason`) |
| Wallet (id, userId unique, balance ≥ 0, lastDailyBonusAt, rowVersion) | ✅ | `Wallet.cs`, `WalletConfiguration` (CHECK, RowVersion) |
| WalletTransaction (type, amount, before/after, betId, idempotencyKey unique, reason, createdAt) | ✅ | `WalletTransaction.cs`, `WalletTransactionConfiguration` |
| Game (name, slug unique, description, type, status, min/max bet, betting duration, pause, createdAt) | ✅ | `Game.cs`, `GameConfiguration` |
| GameRound (gameId, roundNumber, state, serverSeed, serverSeedHash, crashPoint, startedAt, crashedAt) | ✅ | `GameRound.cs` |
| Bet (roundId, userId, amount, slot 1/2, autoCashOutAt, cashOutMultiplier, payout, status, placedAt, cashedOutAt) + unique (roundId, userId, slot) | ✅ | `Bet.cs`, `BetConfiguration` |
| PlayerLimit (userId, dailyBetLimit, sessionReminderMin, selfExcludedUntil) | ✅ | `PlayerLimit.cs` (+ pending increase columns) |
| Notification (userId, type, title, message, isRead, createdAt) | ✅ | `Notification.cs` |
| AuditLog (actorUserId, action, entityType, entityId, oldValue/newValue JSON, createdAt) | ✅ | `AuditLog.cs`; `entityId` is a string on purpose (setting keys like `Wallet.DailyBonusAmount` are not numbers) |
| SystemSetting (key, value, updatedAt) | ✅ | `SystemSetting.cs` (seeded) |
| Relations: User 1–1 Wallet, 1–N Bet, 1–1 PlayerLimit; Game 1–N GameRound 1–N Bet; Wallet 1–N WalletTransaction; Bet 1–N WalletTransaction | ✅ | `Persistence/Configurations/*` |
| Achievement (\*), UserAchievement (\*), ChatMessage (\*) | ✅ tables only | Tables and seeds exist (`Achievement` with 5 seeded rows); features are optional, see below |

Beyond the spec: 19 supporting tables (30 in total), listed in README → Database.

## 7. Quality, security and delivery

### Security

| Item | Status | Where |
|---|---|---|
| Passwords stored only as BCrypt hashes | ✅ | `BCryptPasswordHasher` |
| Short-lived JWT (15 min), refresh token stored hashed | ✅ | `JwtOptions` (15 min), `RefreshToken.TokenHash` (SHA-256) |
| Role-based access on every protected endpoint | ✅ | `[Authorize]` / `[Authorize(Roles)]`, `AuthorizationAttributeTests` |
| Rate limiting on login and bets | ✅ | policies `auth` (10/min/IP) and `bets` (20/10 s/user), `RateLimitAttributeTests` |
| CORS only for the Angular origin | ✅ | `AddAngularCors` (`Cors:AllowedOrigins`) |
| SQL injection protection (EF Core parameterized queries) | ✅ | No raw SQL with user input |
| Balance changes in one DB transaction with RowVersion | ✅ | `Wallet` + `ConcurrencyRetry` |
| Administrative actions written to the audit log | ✅ | `AuditTrail.Entry` in every admin/operator service |

### Error handling and logging

| Item | Status | Where |
|---|---|---|
| Global exception middleware, ProblemDetails responses | ✅ | `API/Middleware/ExceptionHandlingMiddleware.cs` |
| Validation errors per field (400) | ✅ | `FluentValidationFilter` → `ValidationProblemDetails` |
| Serilog: requests, errors, round events, balance operations | ✅ | `UseSerilogRequestLogging`, engine/settlement/wallet/bet/admin services log |
| Angular HTTP interceptor shows errors in plain words | ✅ | `error.interceptor.ts` (ProblemDetails `detail`) |

### Testing

| Item | Status | Where |
|---|---|---|
| xUnit: crash point generation and provably fair verification | ✅ | `ProvablyFairTests`, `MultiplierBucketsTests` (20,000 rounds vs theory) |
| xUnit: round state transitions (illegal ones refused) | ✅ | `GameRoundStateTests` |
| xUnit: wallet (negative balance, double operation, refund) | ✅ | `WalletTests`, `BalanceAdjustmentTests`, `AdminUserServiceTests.AdjustBalance_TheSameRequestIdTwice_IsAppliedOnlyOnce` |
| xUnit: bet and cash-out rules (refused in the wrong phase) | ✅ | `BetTests` |
| Jasmine: Angular stores and services | ✅ | `AuthStore`, `CrashStore`, `NotificationsStore`, `WalletStore`, `LimitsStore`, `PreferencesStore`, `ToastStore`, `ThemeService`, `SoundService`, `LayoutService`, `CrashHub`, loading / error / auth interceptors, ProblemDetails helpers, form validators, guards (112 tests) |
| Integration tests with WebApplicationFactory (\*) | ✅ | `Veltrix.Tests/Integration`: `AuthApiTests`, `BetApiTests`, `AdminApiTests`, `DemoDataSeederTests` (28 tests on a throwaway SQL Server database) — Step 2 |

### Git

| Item | Status | Where |
|---|---|---|
| `main` and `develop` branches, `feature/*` branches | 🟡 | `develop` and `feature/*` as specified; the main branch is called **`master`** (the author's convention in CLAUDE.md) |
| Clear, small, frequent commits | ✅ | `git log` |
| `.gitignore`; secrets (connection string, JWT key) not in the repository | ✅ | JWT key in user-secrets; the connection string uses Windows authentication (no password) |

### Delivery

| Item | Status | Where |
|---|---|---|
| README: description, technologies, architecture | ✅ | `README.md` |
| README: screenshots | 🟡 | Phase 5 (taken when the UI is final) |
| Setup and run instructions (back-end, front-end, database) | ✅ | README → Setup and run |
| DB scripts: migrations + seed script (test users with all three roles, a game) | 🟡 | Migrations ✅ (`dotnet ef migrations script` produces SQL); seed ✅ automatic in development (`HasData` + `DatabaseInitializer`); a standalone seed SQL script is Phase 5 |
| Swagger API documentation | ✅ | `/swagger`, `docs/api/openapi.json` |
| Presentation: demo round in several browsers, provably fair check, admin panel | 🟡 | Phase 5 (the features it shows all exist) |

## 8. Evaluation criteria (spec §8)

| Criterion | Evidence |
|---|---|
| Responsive design (HTML/CSS) | Every page checked at 1440 / 768 / 375 in dark and light (automated overflow sweep), docs/DESIGN.md |
| Component structure (Angular) | Standalone components, `features/*`, reusable `shared/ui` design system + `/styleguide` |
| UI functionality | Canvas curve, validated forms, filters, Chart.js charts |
| State management | NgRx SignalStores for auth, wallet, current round, notifications, limits, preferences; SignalR events flow into the stores |
| Database design (SQL) | 11 core + 19 supporting tables, relations, unique indexes, CHECK constraints |
| EF models and migrations | Code First, Fluent API configurations, 4 migrations, seed |
| REST API | 51 operations, Swagger, ProblemDetails, pagination |
| Authorization / authentication | JWT + rotating refresh tokens, 4 roles, guards, lockout |
| Code quality and documentation | Layered architecture, 246 xUnit (incl. Moq service tests and 28 WebApplicationFactory integration tests) + 112 Jasmine tests, Serilog, README, DESIGN.md, this file |
| Presentation | Phase 5 |

---

## Optional (\*) items

| Item | Spec | Status | Notes / where |
|---|---|---|---|
| Email verification | 4.1 | ❌ | `EmailVerificationCode` table exists |
| Two-factor authentication | 4.1 | ❌ | `UserTwoFactor` table exists |
| Two parallel bets per round | 4.2 | 🟡 | Backend ready (`Bet.MaxSlot = 2`, unique (round, user, slot)); the UI places slot 1 only |
| Auto-bet with a number of rounds | 4.2 | ❌ | `AutoBetConfig` table exists |
| Balance refill when it reaches zero (once a day) | 4.4 | ❌ | |
| Chat with moderation | 4.5, 4.9 | ❌ | `ChatMessage`, `ChatMute` tables exist |
| Achievements / badges | 4.6 | ❌ | `Achievement` (5 seeded), `UserAchievement` tables; no unlock logic or endpoints |
| Levels and XP | 4.6 | 🟡 | `Level` table (10 seeded) and `PlayerStats.Xp/LevelNumber`; XP is never awarded, so everyone is level 1 |
| Georgian and English | 4.7 | ✅ | Whole UI, API messages and Rules pages in Georgian (default) and English; `core/i18n`, `src/i18n/ka.json`, `UserPreference.Language` — Step 3 |
| Page: why Crash has no winning strategy | 4.8 | ✅ | `/rules` renders the seeded `no-winning-strategy` page — **added in this pass** |
| CSV export of reports | 4.10 | ❌ | |
| Email notifications | 4.11 | ❌ | Only the development email sender (logs reset codes) |
| Achievements endpoints `GET /api/achievements`, `/my` | 5 | ❌ | |
| `GET /api/admin/reports/export` | 5 | ❌ | |
| SignalR `ChatMessage` / `SendChatMessage` | 5 | ❌ | |
| Integration tests (WebApplicationFactory) | 7 | ✅ | `Veltrix.Tests/Integration` (28 tests): auth, bets, cash-out, refunds, admin role checks, demo data — Step 2 |
| Docker Compose (SQL Server + API + Angular) | 2 | ❌ | |
| GitHub Actions (build + tests on every push) | 2 | ❌ | |

---

## Gaps closed in this pass

| Gap (non-\*) | Spec | What was added |
|---|---|---|
| No home page (`/` redirected to the game) | 3, 4.7 | `features/home`: hero, live round card (real-time), available games, top-5 leaderboard |
| Games catalog only in the API | 3, 4.7 | `/games` with search + type + status filters (in the URL); `/crash/:slug` plays any Crash game; maintenance notice and "game not found" state |
| Public player profile only in the API | 4.6 | `/players/:nickname`; nicknames link to it from the leaderboard, live bets and home |
| No rules page | 3 (guest rights) | `GET /api/pages/{slug}` (StaticPages table) + `/rules` with a safe Markdown renderer (`vx-markdown`, never `innerHTML`) |
| Settings had the theme only | 4.7 | `GET/PUT /api/profile/preferences`, `/settings` (theme System/Dark/Light, game sounds via Web Audio, animations off = reduced motion), settings follow the account |
| Moq never used | 2, 7 | 16 xUnit + Moq service tests (wallet, limits, admin actions, preferences, content pages, daily-bonus job) on EF Core InMemory |
| Few Jasmine tests for stores / services | 7 | +31 tests: WalletStore, LimitsStore, PreferencesStore, ToastStore, ThemeService, interceptors, Markdown, crash-store sounds |
| No Postman material | 2 (tools) | `docs/api/openapi.json` + import instructions in README |

## Phase 5 Step 2 (quality)

| What | Where |
|---|---|
| 28 integration tests: the real API in memory (`WebApplicationFactory<Program>`) over HTTP, on a throwaway SQL Server database that the migrations create and the run drops | `Veltrix.Tests/Integration/*` |
| Auth: register, field validation errors, duplicate email, login, 401 without / with a forged token, refresh rotation, logout, 30-second reuse window, theft alarm, auth rate limit (11th login → 429, refresh not limited) | `AuthApiTests` |
| Bets: stake + ledger row, refused after betting closes / below minimum / 3 decimals / same slot / above balance, cash-out at the server's multiplier, too late after the crash point, crash settles as lost and reveals the crash point, operator cancel refunds active bets (cash-outs keep their payout), only players bet, blocked and self-excluded players refused | `BetApiTests` |
| Admin role checks: every `api/admin` read endpoint answers 401 / 403 / 403 / 200 to guest / player / operator / admin; only staff cancel rounds; no self role change / self block / self balance adjustment; adjustment writes ledger + audit; a promoted player gets the new role at the next login | `AdminApiTests` |
| **Bug found by the integration tests:** validation errors were sent as `application/json` instead of `application/problem+json` (`[Produces("application/json")]` rewrites every `ObjectResult`); the filter now returns a `JsonResult` with the right type. The body was already correct, so the Angular client is unaffected | `FluentValidationFilter` |
| +17 Jasmine tests: auth interceptor (token only on API calls, ONE refresh + retry after 401, failed refresh ends the session, no refresh for guests / 403), ProblemDetails helpers, form validators, `SoundService`, `LayoutService` | `auth.interceptor.spec.ts`, `problem.spec.ts`, `validators.spec.ts`, `sound-layout.spec.ts` |
| **Development demo seed:** 15 demo players (`<nickname>@demo.veltrix.dev` / `Player123!`) with 30 days of play (≈ 230 rounds, 1300 bets), played by the real domain and settlement code with a clock set to the past: real seeds and crash points, ledger rows for every bet / win / daily bonus, cash-outs below the crash point, PlayerStats and daily statistics recalculated. Runs once, only with `Database:SeedDemoData` (Development). `DemoDataSeederTests` checks the rules on SQL Server | `Persistence/DemoDataSeeder.cs`, `appsettings.Development.json` |
| Warnings: `dotnet build --no-incremental` 0, `ng build` 0; the Karma log no longer shows a 404 for the web font (unit tests do not load fonts) | `angular.json` (test target) |

## Phase 5 Step 3 (optional features)

| Feature | What | Where |
|---|---|---|
| Preparation | The top-bar dropdowns (account menu, notifications) load on first use: the CDK Overlay/Menu left the first download (499 → 405 kB), making room for the features below in the 500 kB budget | `layout/top-bar-popups.ts` |
| **(1) Georgian + English** | Georgian is the default, English the second language; instant switch (no reload) in Settings → Language, the account menu, and a top-bar button for guests; saved on the account (`GET/PUT /api/profile/preferences` → `language`, validated `ka`/`en`); existing accounts moved to `ka` by the migration `GeorgianDefaultLanguage` | `core/i18n/*`, `PreferencesStore`, `settings-page.ts`, `UserPreference` |
| | ALL Georgian text in one file `Veltrix.Web/src/i18n/ka.json` (685 UI texts, every API message, the two Rules pages), natural Georgian; Crash, Cash out and VLX kept | `src/i18n/ka.json` |
| | API messages (errors, notifications, ledger reasons, audit reasons, level titles) shown in Georgian by the client, with placeholders for changing values | `I18n.server()`, `serverText` / `knownText` pipes |
| | Georgian dates (month names), browser tab titles, Noto Sans Georgian font, layout fixes for longer text | `vxDate` pipe, `TranslatedTitleStrategy`, DESIGN.md §8b |
| | Guards: `npm run i18n:check` (0 missing / 0 unused), Jasmine `i18n.spec.ts` (+12), xUnit `GeorgianTranslationTests` (every user-facing API message has Georgian), integration `PreferencesApiTests`; browser sweep 132 loads in each language at 1440 / 768 / 375, both themes | `scripts/i18n-check.mjs`, tests |
| | **Bug found and fixed:** the content-page fallback used the UI default language, so when it became Georgian every Rules page answered 404; the fallback is now always English (`ContentPage_IsServedFromTheSeededTable_WithEnglishAsTheFallback` failed before the fix) | `StaticPageService` |

## Decisions to review

Choices made while working without the author. Each one follows the spec and DESIGN.md as closely as possible; all are easy to change.

| Decision | Why | How to undo |
|---|---|---|
| The spec PDF is now in the repository as `docs/SPEC.pdf` (copied from the Desktop) | The Phase 5 instructions refer to `docs/SPEC.pdf`, and the exam copy of the repository should contain its own specification | `git rm docs/SPEC.pdf` |
| Integration tests use a real SQL Server database, not EF InMemory or SQLite | Bets use SQL Server features that the other providers do not have (REPEATABLE READ transactions, `ExecuteUpdate`, `rowversion` concurrency, CHECK constraints). The test database `Veltrix_Tests_<random>` is created by the migrations and dropped at the end; the tests refuse to run if they are connected to any other database | Set `VELTRIX_TEST_SQL` to point at another server |
| In the integration tests the crash engine's background loop is off; tests move rounds with `RoundSettlementService` | The engine's timing is random (crash point from the seed); with the loop off a test decides exactly when betting closes and the round starts, and uses the same code the engine uses | — |
| Validation errors now have the content type `application/problem+json` | Every other error already had it (RFC 7807, spec §7 "ProblemDetails"); the body is unchanged | Revert `FluentValidationFilter` |
| Branch `master` instead of the spec's `main` | The author's convention (CLAUDE.md) | `git branch -m master main` |
| Tailwind CSS v4 (plain CSS) instead of SCSS | Tailwind v4 is configured in CSS and does not support Sass; DESIGN.md decision | — |
| Leaderboard "profit" = net result (payouts − stakes) | The spec says "biggest virtual win"; a net result cannot be inflated by betting a lot and losing | `LeaderboardService` |
| `AuditLog.entityId` is a string | Setting keys such as `Wallet.DailyBonusAmount` are not numbers | — |
| Paged endpoints use `pageSize` (spec examples sometimes say `size`) | One name on every paged endpoint | — |
| The demo seed runs automatically on the first Development start (`Database:SeedDemoData: true`) and also ran on the existing development database | The step asked for a non-empty leaderboard, charts and dashboard in development; it only ADDS rows (15 users, their rounds, bets and ledger) and never touches existing accounts (a nickname that is already taken is skipped) | Set `SeedDemoData` to `false`; the demo players can be blocked from the admin panel |
| On a database that already had rounds, the demo rounds got the next round numbers although they are dated in the past (on a fresh database the numbers follow the dates) | Round numbers must stay unique and increasing per game; renumbering existing rounds would change real data | — (only the order of the round history looks unusual on the existing development database) |
| Demo players are ordinary Player accounts with the same password as the test players (`Player123!`) | Easy to log in as one during the demo to show filled-in profile charts | — |
| Translation keys are the English texts themselves (no `crash.betPanel.title`-style ids); English needs no file | Templates stay readable, and a missing translation shows English instead of a broken id. `npm run i18n:check` keeps code and ka.json in step | — |
| The Georgian Rules pages are in `ka.json` (`pages`), not as `ka` rows of the StaticPages table | Your instruction: ALL Georgian text in one file. The table and `GET /api/pages/{slug}?lang=` still support languages | Add `ka` rows to StaticPages and drop `pages` from ka.json |
| API messages are translated in the browser (ka.json `server`), the API itself stays English | Keeps the one-file rule and the API simple; unknown messages (e.g. free text an admin typed) stay as written | Server-side localization (resx) |
| Existing accounts were switched to `ka` by a migration | Nobody could choose a language before, so the stored `en` was only the old default | — |
| Amounts and multipliers keep the international format (`1,234.56`, `2.35x`) in Georgian too; dates are Georgian | One number format across the game (players compare multipliers); Georgian betting sites show multipliers the same way | Use `formatNumber` with the `ka` locale |
| Kept untranslated besides Crash / Cash out / VLX: "Provably fair" (nav and badge) and "seed" | Established technical terms of crash games; the home page explains them in Georgian ("დამტკიცებადი სამართლიანობა") | Edit ka.json |
| "Responsible play" is "უსაფრთხო თამაში" (not "პასუხისმგებლიანი თამაში") | The longer phrase did not fit the sidebar; both are common | Edit ka.json |

## Still open (non-\*)

| Item | Why | When |
|---|---|---|
| README screenshots | Taken once the UI is final | Phase 5 |
| Standalone seed SQL script | The database is seeded automatically in development; a script for a fresh server is planned | Phase 5 |
| Presentation demo | The author's exam preparation | Phase 5 |
| Branch name `master` instead of `main` | The author's convention (CLAUDE.md); a rename is one command if wanted | Author's decision |
| SCSS | Tailwind CSS v4 is configured in CSS and does not support Sass; chosen deliberately for the design system | Decision (docs/DESIGN.md) |

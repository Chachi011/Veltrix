# VELTRIX: exam presentation

A 10-minute live demo, then the 10 questions most likely to come up, each with a short answer you can say in a few sentences.

> One sentence to start with: **"VELTRIX is a real-time multiplayer Crash game where the server owns every round, every round can be checked by the player, and only virtual VLX points exist."**

---

## Before the exam (15 minutes earlier)

| Check | How |
|---|---|
| SQL Server is running | `sqlcmd -S localhost -E -Q "SELECT 1"` |
| API is running | `dotnet run --project Veltrix.API --launch-profile http` → http://localhost:5295/swagger opens |
| Angular is running | `cd Veltrix.Web && npm start` → http://localhost:4200 |
| Window 1 (normal browser) | logged in as `player1@veltrix.dev / Player123!`, Crash page |
| Window 2 (private window) | logged in as `player2@veltrix.dev / Player123!`, Crash page |
| Window 3 (another browser or profile) | logged in as `admin@veltrix.dev / Admin123!`, `/admin` |
| Phone (optional) | same Wi-Fi, or Chrome DevTools device mode (375 px) |
| Language | Georgian is the default; the top-bar / Settings switch changes it live, so you can present in either |
| Backup | screenshots in `docs/screenshots/`; tests can be run live (`dotnet test`, `npx ng test`) |

Tip: the login page has one-click buttons for the test accounts (development only).

---

## The 10-minute demo

| Time | What you show | What you say |
|---|---|---|
| **0:00–1:00** Introduction | Home page (live round card, top players of the day), the README architecture diagram | "Angular 20 client, ASP.NET Core 8 API, SQL Server, SignalR for real time. Four layers: Domain (rules, no packages), Application, Infrastructure, API. Virtual points only: no deposits, no withdrawals, no real money." |
| **1:00–3:30** A live multiplayer round | Windows 1 and 2 side by side. Player 1 bets 50 VLX with auto cash-out 2.00x; player 2 bets 20 VLX. Both bets appear in both windows. Player 2 presses **Cash out** by hand; the round crashes | "The countdown, the curve and the multiplier come from the server through SignalR every 100 ms. When I press Cash out, the browser only sends *the request*; the server pays my stake × the multiplier on *its own clock*. The browser never sends a multiplier or a result." Show both balances updating and the History page |
| **3:30–5:00** Provably fair | The hash badge next to the round number (visible during betting). After the crash: Provably fair page → verify the round | "Before betting opens, the server has already chosen a random seed and the crash point, and it publishes the SHA-256 hash of the seed. After the crash it reveals the seed. My browser recalculates the hash and the crash point itself (Web Crypto API). If the server had changed anything, the hash would not match." |
| **5:00–6:00** The server owns the rules | Swagger (Authorize with player 1's token): `POST /api/bets` during the flight → 400 "Betting is closed for this round."; a bet above the balance → 400 "Insufficient balance: …" | "Every rule is checked on the server: bets only while betting is open, cash-out only while the round runs, the balance can never go negative, also as a CHECK constraint in the database." |
| **6:00–7:00** Achievements and levels | Profile → Level and achievements (safety and learning first); open the Provably fair page → bell: "Achievement unlocked" | "XP never comes from betting: a bet gives none. It comes only from one-time tasks: set a limit, read the rules, verify a round, try the game. Playing alone reaches level 4 of 10; the top needs the learning and safety tasks. The server awards them, with a ledger row, so a reward is never paid twice." |
| **7:00–8:30** Operator and admin | Window 3: Operator → cancel a running round with a reason → players get their stakes back (Wallet: "Round #N cancelled"). Admin → Users → block player 2 → window 2 is signed out at once. Audit log shows both actions with old and new values. Dashboard charts | "Roles are checked on the server with `[Authorize(Roles = …)]`; the Angular guards only hide pages. Every admin action is audited." |
| **8:30–9:15** Responsible play | Responsible play page: daily limit, self-exclusion with its confirmation; then the Crash page ("Betting paused until …"), the games list (Watch instead of Play) and the profile (no play tasks); Rules → why there is no winning strategy | "A lower limit applies at once, a higher one waits 24 hours. A self-exclusion cannot be shortened. Both are enforced by the server on every bet, and while a player cannot bet, nothing in the app invites them to play." |
| **9:15–10:00** Quality and wrap-up | Phone view (bottom navigation, bet sheet), language switch, the test summary | "304 xUnit tests (39 of them run the real API against a throwaway SQL Server database) and 134 Jasmine tests; 0 build warnings; the whole UI in Georgian and English." |

If something breaks during the demo: SignalR reconnects by itself (banner at the top); a page reload is always safe because the server holds all state.

---

## The 10 most likely questions

**1. How do you stop a player from cheating the multiplier or the cash-out?**
The client never sends a result. It sends "place a bet" or "cash out", nothing else. The server checks the round state (`GameRound` state machine: bets only in BettingOpen, cash-out only in Running) and calculates the multiplier itself: `m = e^(0.00006·ms)` from the server's clock. A late request after the crash point is refused.

**2. How does "provably fair" work, and why can't the server cheat?**
Before the round the server picks a random 32-byte seed, calculates the crash point from `HMAC-SHA256(seed, "VELTRIX")` and publishes `SHA-256(seed)`. After the crash it reveals the seed. Anyone can recalculate both. To change the crash point the server would need another seed with the same SHA-256 hash, which is practically impossible.

**3. What is the house edge, and how do you know the numbers are right?**
The formula `max(1, floor(100·0.97/(1−r))/100)` gives a 3% edge: the chance that a round reaches x is `0.97/x`. A unit test generates 20,000 rounds and compares them with the theory, and the admin dashboard shows the real distribution of the last 1000 rounds next to the theoretical one.

**4. Two requests change the same wallet at the same moment. What happens?**
Every balance change is one database transaction with a `WalletTransaction` row (balance before/after, unique idempotency key). `Wallet.RowVersion` is a concurrency token: if someone else changed the wallet in the meantime, EF Core throws, and `ConcurrencyRetry` repeats the operation with fresh data. The database also has `CHECK (Balance >= 0)`, so even a bug cannot make it negative.

**5. What if the server stops in the middle of a round?**
On startup the engine cancels every unfinished round and refunds all active bets (cash-outs that already happened keep their payout). The crash point was fixed before the round, so nothing can be "re-rolled".

**6. How does login work and how do you protect the tokens?**
A short-lived JWT access token plus a refresh token. Refresh tokens rotate on every use; an old token replayed later is treated as theft and every session is revoked (a 30-second grace period covers two tabs refreshing at once). Passwords are hashed with BCrypt, login is rate-limited (10 per minute per IP) and locked after repeated failures. The JWT key lives in user-secrets, never in Git.

**7. Why this architecture (Domain / Application / Infrastructure / API)?**
Dependencies point inward: the Domain knows nothing about the database or the web. So the important rules (wallet, bets, round states, limits) are plain C# classes that unit tests check without a database. The database and web parts can change without touching the rules.

**8. How does the real-time part work?**
One SignalR hub. Each game is a group (round state, multiplier ticks every 100 ms, bets, cash-outs); each user also gets personal messages (balance, notifications). The browser opens one connection per tab and reconnects automatically. Between ticks it draws the curve smoothly from the server's start time.

**9. How did you test it?**
Four levels: xUnit for the domain rules; xUnit + Moq for services; WebApplicationFactory integration tests that call the real API over HTTP against a throwaway SQL Server database (auth, bets, cash-outs, refunds, admin role checks, achievements); and Jasmine for the Angular stores, interceptors and guards. Extra guards check that every UI and API text has Georgian, that no protected endpoint is accidentally public, and that the delivered SQL script contains every migration.

**10. Why virtual points, and what about responsible play?**
It is an educational project: gambling with real money needs a licence and can cause harm, so there are no deposits, purchases or withdrawals. VLX comes only from the starting balance, the daily bonus and achievements. Players still get real protection tools: a daily bet limit, self-exclusion, a session reminder and a page that explains why Crash has no winning strategy. The game design follows the same rule: XP and achievements never reward how much or how long someone bets (only one-time learning, safety and "try it" tasks), and while a player is self-excluded or at their daily limit, nothing in the app invites them to play.

---

## Useful commands during questions

```bash
dotnet test                                             # 304 backend tests
cd Veltrix.Web && npx ng test --watch=false --browsers=ChromeHeadless   # 134 frontend tests
cd Veltrix.Web && npm run i18n:check                    # every text has Georgian
git log --oneline --graph -15                           # branch history
git show v1.1-diploma                                   # the submitted version (v1.0-diploma = the first one)
```

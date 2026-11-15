# VELTRIX design system

The single source of truth for every UI decision. **Any change to a design decision updates this file in the same commit.**

The look: a premium, modern fintech/gaming dashboard, not a template. Calm surfaces, one accent, numbers that are easy to read.

Implementation: tokens and base styles in [`Veltrix.Web/src/styles.css`](../Veltrix.Web/src/styles.css), and components in [`Veltrix.Web/src/app/shared/ui/`](../Veltrix.Web/src/app/shared/ui/). The living reference is the `/styleguide` route (development builds only), which shows every component in both themes.

---

## 1. Stack

| Concern | Choice |
|---|---|
| Styling | **Tailwind CSS v4**, configured in CSS (`@theme` in `styles.css`), loaded through PostCSS (`.postcssrc.json`) |
| Overlays | **Angular CDK**: Dialog (modals and drawers), Menu (account menu), Overlay (notifications panel). The top-bar dropdowns live in `layout/top-bar-popups.ts`, loaded on the first click (prefetched when the browser is idle), so the CDK Overlay and Menu code (~75 kB) is not part of the first download |
| Charts | **Chart.js 4** wrapped in `vx-chart` (token colors, design-system fonts). Loaded with a dynamic `import()` on first use, so pages without charts never download it |
| Icons | **lucide-angular**. Only the icons listed in `shared/ui/icons.ts` are bundled |
| Fonts | **Inter** (UI) and **JetBrains Mono** (all numbers), the Google Fonts families served from npm (`@fontsource-variable/*`) so they also work offline. **Noto Sans Georgian** for Georgian letters (Inter has none); its `unicode-range` means only pages that show Georgian download it |
| Not allowed | Bootstrap, the default Angular Material look, any other component library |

The global stylesheet is plain CSS (`styles.css`), because Tailwind v4 does not support Sass. Component styles may use SCSS but must not use Tailwind directives.

---

## 2. Tokens

Tokens are CSS variables defined on `[data-theme='dark']` / `[data-theme='light']` (and `:root` = dark). They work on `<html>` **and on any element**, so a subtree can use the other theme (the styleguide relies on this). Components use only tokens, never hex values.

### 2.1 Colors

| Role | Dark (exact spec) | Light | Used for |
|---|---|---|---|
| `--bg` | `#0B0E14` | `#F6F7FB` | Page background |
| `--surface` | `#121722` | `#FFFFFF` | Cards, sidebar, menus |
| `--surface-2` | `#1A2130` | `#F1F4F9` | Inputs, hover, secondary buttons |
| `--border` | `#252D3D` | `#E2E8F0` | All 1px borders |
| `--text` | `#E6EAF2` | `#0F172A` | Main text |
| `--text-muted` | `#8A93A6` | `#5B6478` | Labels, hints, secondary text |
| `--primary` | `#7C5CFF` | `#7C5CFF` | Violet: main actions |
| `--accent` | `#22D3EE` | `#0E7490` | Cyan: multiplier curve, highlights, ≥10x pills |
| `--success` | `#22C55E` | `#15803D` | Cash out, wins |
| `--danger` | `#EF4444` | `#DC2626` | Crash, losses, errors |
| `--warning` | `#F59E0B` | `#B45309` | Warnings |

### 2.2 Derived tokens (WCAG AA)

The spec colors are used exactly for fills, the curve and borders. Where a spec color fails AA **as text**, a derived token is used for text only:

| Token | Dark | Light | Why |
|---|---|---|---|
| `--primary-text` | `#A996FF` | `#6A48F5` | `#7C5CFF` text on `#121722` is 3.7:1 (needs 4.5); `#A996FF` is 7.2:1 |
| `--primary-hover` | `#8F74FF` | `#6A48F5` | Hover of primary fills |
| `--on-primary` | `#FFFFFF` | `#FFFFFF` | Text on violet (4.8:1) |
| `--on-success` | `#0B0E14` | `#FFFFFF` | White on `#22C55E` is only 2.3:1, so dark text in the dark theme |
| `--on-accent`, `--on-warning`, `--on-danger` | `#0B0E14` | `#FFFFFF` | Same reason |

The light theme uses darker shades of the same hues for accent/success/danger/warning, because the dark-theme values on white fail AA (e.g. `#22D3EE` on white = 1.8:1).

### 2.3 Tailwind mapping

`@theme inline` maps tokens to utilities. **The default Tailwind palette is switched off** (`--color-*: initial`), so `bg-gray-500` and similar classes simply do not exist.

| Utility family | Values |
|---|---|
| Colors | `bg`, `surface`, `surface-2`, `border`, `text`, `muted`, `primary`, `primary-hover`, `primary-text`, `on-primary`, `accent`, `on-accent`, `success`, `on-success`, `danger`, `on-danger`, `warning`, `on-warning`, `white`, `black` |
| Tints | Opacity modifiers on tokens: `bg-primary/12`, `bg-danger/10`, `border-accent/30` |
| Fonts | `font-sans` (Inter), `font-mono` (JetBrains Mono) |
| Radius | `rounded-card` (12px), `rounded-control` (10px) |
| Shadows | `shadow-soft`, `shadow-card`, `shadow-pop` |
| Custom utilities | `num` (mono + tabular numbers), `glass` (70% surface + blur) |

Rule: write **complete class strings** (e.g. a `Record<Variant, string>`), never build them like `'bg-' + tone`, because Tailwind only generates classes it can find literally in the source.

### 2.4 Spacing, radius, borders, shadows

- **8px grid.** Use even Tailwind steps: `2` (8px), `4` (16px), `6` (24px), `8` (32px). Half steps (`1`, `3`) only inside small controls.
- **Radius:** cards, panels, modals and menus 12px (`rounded-card`); buttons and inputs 10px (`rounded-control`); pills and badges fully round.
- **Borders:** always 1px `--border`.
- **Shadows:** very soft. Cards `shadow-card`, floating layers (menus, toasts, modals) `shadow-pop`, buttons at most `shadow-soft`.

---

## 3. Typography

| Use | Style |
|---|---|
| Body | Inter, 14px (`text-sm`), line-height 1.5 |
| Page title | 24–28px semibold, tracking slightly tight |
| Card / section title | 15px semibold |
| Labels | 13px medium, `text-muted` (`.vx-label`) |
| Hints, table headers | 11–12px; table headers uppercase, `tracking-wide`, muted |
| **All numbers** (balance, multiplier, amounts, countdowns, hashes) | `num` utility: JetBrains Mono, `tabular-nums`, so digits don't shift while they change |
| Live multiplier | 64–96px mono semibold |
| Georgian text | Same sizes and weights; the browser takes the Georgian letters from Noto Sans Georgian (next in the `--font-sans` stack) |
| Big number + unit | The unit is smaller and muted ("5 s" / "5 წმ" countdowns), so a Georgian unit does not dominate the number |

Never: gradients on text, all-caps body text, more than two font families.

---

## 4. Layout

| Element | Rule |
|---|---|
| Content width | max **1440px**, centered |
| Page padding | **24px** desktop, **16px** below 768px |
| Breakpoint | **768px** (`md`) switches between phone and desktop layouts |
| Sidebar (≥768px) | fixed left, 240px expanded / 72px collapsed (icons only, with `title` tooltips), `bg-surface`, right border; state remembered in `localStorage` (`vx.sidebarCollapsed`); bottom: disclaimer + collapse button. Items depend on the role; staff tools (Operator, Admin) sit under a small uppercase **"Staff"** heading (a divider when collapsed) |
| Top bar | sticky, 64px, **glass**, bottom border. Left: page title (desktop, visual only: `aria-hidden`, the page's own `<h1>` is the real heading) or logo (phone). Right: balance chip (animated, gem icon) → daily bonus (button when claimable, countdown chip otherwise) → notifications bell (primary badge with the unread count, "9+" above 9) → avatar menu. Guests: theme toggle, Log in, Register |
| Sidebar items | Home, Play, Games, Leaderboard, History\*, Wallet\*, Responsible play (players), Provably fair, Rules, then "Staff" (Operator, Admin). \* logged in. Home is active only on `/` exactly (`routerLinkActiveOptions: { exact: true }`); the logo links to Home |
| Account menu | Profile, Wallet, History, then the role's extra pages (Responsible play, Games, Provably fair, Rules, Settings, Operator, Admin), theme, Log out. On phones it is the only way to reach the pages that are not in the bottom nav |
| Notifications panel | CDK overlay under the bell, 360px: header with the unread count and "Mark all read"; newest first; unread rows tinted `bg-primary/5` with a dot; an icon per type in a tinted circle; a click marks it read and follows the type's link. Escape and an outside click close it |
| Page header | every page starts with `<vx-page-header title description>`: the `<h1>` is visible on phones and visually hidden from 768px (the top bar shows the title), the description is always visible; actions go in `[page-actions]` |
| Bottom nav (<768px) | fixed, glass, 64px + safe-area inset, max 5 items (icon + 11px label); active = `text-primary-text`. Logged in: Home, Play, Top (leaderboard), Wallet, Profile. Guests: Home, Play, Top, Rules, Log in |
| Page bottom padding on phones | enough for the bottom nav (`pb-28`) |
| Bet panel on phones | becomes a **bottom sheet** (Crash page, step 3) |

The navigation items are defined once in `layout/nav-items.ts`, for both the sidebar and the bottom nav.

**Grid/flex children that contain tables or long content need `min-w-0`**; otherwise they grow wider than the screen. `vx-card` and `vx-stat-card` already include it.

Responsive rules learned in Phase 4:

- **Hide a component on phones with `max-sm:hidden`, not `hidden sm:inline-flex`.** `vxButton` and `vx-badge` set their own `display`, which beats a plain `hidden` in the CSS order; a responsive variant comes later and wins.
- **Truncate table text on an inner block** (`<td><div class="max-w-[240px] truncate">…</div></td>`). Browsers ignore `max-width` on a table cell, so long text widens the whole table.
- **Narrow tables move secondary columns into the first cell** (e.g. the audit log shows "by admin" and the action badge under the date on phones) instead of scrolling sideways.
- **Code in the initial bundle (layout, core) imports single files from `shared/ui`, never the barrel**: Angular components are not tree-shaken out of a barrel, so one barrel import would pull every component into the first download.
- **Eager code never imports a CDK overlay module** (`@angular/cdk/overlay`, `menu`, `dialog`): open them from a lazily imported file, as the top bar (`top-bar-popups.ts`) and the session reminder do. The buttons that open them carry `aria-haspopup` and `aria-expanded`; Escape closes and returns the focus to the button.

---

## 5. Components (`shared/ui`)

Import from the barrel: `import { Button, Card } from '../../shared/ui';`

| Component | Selector / usage | Notes |
|---|---|---|
| Button | `<button vxButton variant size [loading] [disabled] [block]>` (also on `<a>`) | Styles the **native** element (keeps keyboard and form behavior). Variants: `primary`, `secondary`, `outline`, `ghost`, `success`, `danger` (soft: red text on a red tint, for AA). Sizes: `sm` 32px, `md` 40px, `lg` 48px, `xl` 56px, `icon` 40px, `icon-sm` 32px. `loading` shows a spinner and disables |
| Card | `<vx-card title subtitle padding="default\|none">`, actions in `[card-actions]` | `padding="none"` for edge-to-edge tables. The header wraps: actions move under the title when there is no room (phones) |
| Field | `<vx-field label for hint [control] patternMessage>` + projected `<input class="vx-input">` | Shows the first error once touched (same messages as `controlErrorMessage`). Uses Default change detection on purpose (form state is not a signal) |
| Input classes | `.vx-input`, `select.vx-input`, `.vx-input-wrap` + `.vx-input-icon` / `.vx-input-suffix`, `.vx-checkbox`, `.vx-label` | Focus: primary border + 4px soft ring. Invalid + touched: danger border |
| Table | `<div class="vx-table-wrap"><table class="vx-table">`; add `vx-table-compact` in narrow columns | Sticky uppercase header, row hover, `text-right` + `num` for numeric columns. On phones, hide secondary columns (`hidden md:table-cell`) instead of forcing horizontal scroll |
| Switch | `<input type="checkbox" class="vx-switch">` | On/off toggle that keeps native checkbox semantics (keyboard, forms) |
| Password input | `<vx-password-input><input class="vx-input" type="password" …></vx-password-input>` | Adds a show/hide button; the projected input keeps `formControlName` and `autocomplete` |
| Page header | `<vx-page-header title description>` + `[page-actions]` | See §4 |
| Tabs | `<vx-tabs [items] [(value)] label>` | Segmented control, `role="tablist"`, ← → keys move between tabs. `label` names the group for screen readers when a page has more than one (leaderboard: "Period", "Ranking"). Section navigation with real URLs (admin) uses the same look with links (`vx-admin-nav`, `aria-current="page"`) |
| Pagination | `<vx-pagination [page] [totalPages] [totalCount] (pageChange)>` | Card footer: "Page 2 of 7 · 98 results" + Previous / Next; hidden for a single page |
| Badge | `<vx-badge tone [dot]>` | Tones: `neutral`, `primary`, `accent`, `success`, `danger`, `warning` |
| Avatar | `<vx-avatar [src] name size>` | `xs` 24, `sm` 32, `md` 40, `lg` 64, `xl` 96. Falls back to initials on a hue derived from the name (also when the image fails) |
| Stat card | `<vx-stat-card label [value] decimals unit [icon] [delta] hint>` | `value = null` → skeleton. Value uses the animated number |
| Skeleton | `<vx-skeleton width height shape="line\|rect\|circle">` | Shimmer; **used instead of spinners** for loading content |
| Empty state | `<vx-empty-state [icon] title message>` + optional projected action | Icon + title + ONE helpful line |
| Animated number | `<vx-animated-number [value] decimals prefix suffix [highlight]>` | Counts to the new value (650ms ease-out cubic) and tints green (up) or red (down) briefly. Instant under reduced motion |
| Copy button | `<vx-copy-button [text] label>` | Check mark for 1.5s + `aria-live` message |
| Hash badge | `<vx-hash-badge [hash] [roundId] label>` | Shield icon, "Provably fair", truncated hash `a9030b56…e574`, copy button; links to `/fairness?round=` when `roundId` is set |
| Multiplier pill | `<vx-multiplier-pill [value] [cancelled]>` | **Red < 2x, neutral 2x–10x, accent ≥ 10x**, "—" for cancelled |
| Progress ring | `<vx-progress-ring [value]="0..1" size stroke color>` + centered content | Betting countdown |
| Progress bar | `<vx-progress-bar [value]="0..1" tone label>` | 8px track (`surface-2`), filled in a token tone, `role="progressbar"`. Daily limit usage: primary < 70%, warning ≥ 70%, danger at 100% |
| Level progress | `<vx-level-progress [profile]>` | Level + title, XP in mono, accent progress bar to the next level, "N XP to level L · title" or "Top level reached" (§9b) |
| Chart | `<vx-chart type="bar\|line\|doughnut" [labels] [series] ariaLabel [format] [formatRight] [height] [legend] stacked>` | Chart.js with token colors read from CSS (follows the theme, also inside a themed subtree), Inter labels, JetBrains Mono numbers, faint grid (muted 14%), card-style tooltip, legend bottom-left with dots. Series: `{ label, data, tone, kind?: 'bar'\|'line', axis?: 'left'\|'right', fill?, tones? }` (mixed bar + line, a second axis, a tone per bar or slice). Lines use monotone interpolation (smooth but never beyond the real values). 500ms animation, none under reduced motion. `ariaLabel` is required (`role="img"`) |
| Markdown | `<vx-markdown [source]>` | Content pages (rules): paragraphs, numbered / bulleted lists (`marker:text-primary-text`), **bold** (`text-text`), *italic*; body text muted. Parsed into data and drawn by the template, **never `innerHTML`**, so text from the database cannot inject markup. The inline template is one line (line breaks would become spaces before punctuation) |
| Drawer | `ModalService.openDrawer(Component, { data, ariaLabel, width })`; content in `<vx-drawer title description>` with `[drawer-footer]` | Panel from the right, full height, 480px (full width on phones), solid `bg-surface` + left border + `shadow-pop`, slides in (`animate-drawer-in`, 250ms). Same CDK focus trap, Escape and backdrop as a modal. For detail views with actions (admin user) |
| Theme toggle | `<vx-theme-toggle>` | Sun / moon icon button |
| Modal | `ModalService.open(Component, { data, ariaLabel, width })`, `ModalService.confirm({ title, message, confirmText, tone })`; dialogs wrap content in `<vx-modal title description>` with `[modal-footer]` | CDK Dialog: focus trap, Escape, restores focus. Glass panel, blurred backdrop |
| Toasts | `ToastStore.success / error / info(message)` (the API error interceptor calls `error`) | Top on phones, bottom-right on desktop; icon per kind; errors use `role="alert"` and stay longer (6s vs 3.5s). Always above dialogs and drawers (`z-[1100]`; the CDK overlay is 1000), so an error is never hidden behind an open panel |
| Icons | `<lucide-icon [img]="icons.wallet" [size]="18" />` with `protected readonly icons = Icons` | Add new icons to `icons.ts` only. Sizes: 14 (dense), 16 (buttons, menus), 18 (nav, icon buttons), 20–22 (empty states, bottom nav). **VLX amounts use `icons.vlx` (a gem)**: a coin would suggest money, and Lucide's coins icon reads like a chain link at 16px |

Rules:
- Use these components on new and rebuilt pages. Don't create one-off styled buttons, cards or inputs.
- A new reusable visual element becomes a component in `shared/ui`, is added to `/styleguide`, and is documented here.

---

## 6. Glass, glow, gradients

- **Glass** (`glass` utility: 70% surface, `backdrop-blur`) **only** on the top bar, the bottom nav and modals.
- **Glow**: one accent glow, **only on the live multiplier** while the round is running (`[text-shadow:var(--vx-glow)]`).
- **Gradients / fills**: only the soft fill under the multiplier curve, the soft 12% fill under a chart line (`fill: true`) and the skeleton shimmer. No gradient text, no neon.
- Drawers are not glass: they hold forms and tables, which read better on a solid surface.

---

## 7. Motion

| Rule | Value |
|---|---|
| Hover / focus / state transitions | **150–250ms, ease-out** (`duration-200 ease-out-soft`, i.e. `cubic-bezier(0.22, 1, 0.36, 1)`) |
| Overlays appearing | `animate-pop-in` (menus, modals, notifications panel, unread badge, 200ms), `animate-slide-up` (toasts, 250ms), `animate-drawer-in` (drawers, 250ms) |
| Charts | Chart.js animation 500ms on first draw and on data changes |
| Balance changes | animated number (650ms) |
| Crash | multiplier turns `--danger` + `animate-shake` (450ms, once) |
| Loading | skeleton shimmer, no spinners (the button's own loading spinner is the exception) |
| **prefers-reduced-motion** | all animations and transitions become instant; the animated number jumps; no shimmer and no shake |
| **Settings → Animations off** | exactly the same as the OS setting: `PreferencesStore` sets `data-motion="reduce"` on `<html>`, `styles.css` repeats the reduced-motion rules for it, and JavaScript animations ask `reducedMotion()` (`shared/motion.ts`), which checks both |
| Sound | Off by the player's choice (Settings → Game sounds, on by default). Three short Web Audio sounds, only for the player's OWN actions: bet accepted (soft double click), cash-out (rising chime, once even if the live event and the HTTP answer both arrive), round lost with my bet in it (low thud). Never for other players, never in a loop |

---

## 8. Accessibility (WCAG AA)

- Text contrast ≥ 4.5:1 (large text ≥ 3:1); non-text graphics ≥ 3:1 (see §2.2).
- **Visible focus everywhere**: global `:focus-visible` outline (2px `--primary-text`, 2px offset); inputs also get the soft ring.
- Icon-only buttons always have an `aria-label`; decorative icons `aria-hidden="true"`.
- "Skip to content" link as the first focusable element.
- Menus and dialogs use the CDK (keyboard navigation, focus trap, Escape).
- Errors: `role="alert"`; live values that change constantly (the multiplier) are **not** announced on every tick.

---

## 8b. Language (Georgian and English)

| Rule | Value |
|---|---|
| Default | **Georgian**; English is the second language. `<html lang>` follows the choice |
| Switch | Guests: a ghost button in the top bar (languages icon + "EN" / "KA"). Logged in: Settings → Language (tabs, each language named in its own language: ქართული / English) and an item in the account menu. Switching is instant (no reload) and is saved on the account |
| Where the text lives | The code is written in English and shows text through the `t` pipe; **all Georgian text is in one file, `src/i18n/ka.json`** (UI texts, API messages, Rules pages). A missing translation shows the English text |
| Terms kept as they are | **Crash, Cash out, VLX** (and "Provably fair", "seed" on the fairness page) |
| Numbers | Amounts and multipliers keep one international format in both languages (`1,234.56 VLX`, `2.35x`, mono); dates use the language's month names |
| Longer text | Georgian is about 30% longer: rows of buttons and filters use `flex-wrap`, labels never get fixed widths, the top-bar bonus button is icon-only below 1024px, sidebar labels are kept short ("უსაფრთხო თამაში") |
| Word order | A sentence with a highlighted value is translated whole (`tSplit`), because Georgian puts suffixes after the number ("Cash out 2.00x-ზე") |

---

## 9. Crash game page

- Large Canvas: the curve in `--accent` (turns `--danger` after the crash) with a soft fill below, faint grid (muted at 14%), mono axis labels. Canvas needs concrete colors, so tokens are read with `getComputedStyle` and converted to rgba.
- Huge centered multiplier (mono, 64px phones / 96px desktop): `--text` with the accent glow while running; on crash `--danger` + one shake, label "CRASHED".
- Betting phase: countdown progress ring (112px) with seconds inside.
- Bet panel: amount with ½ / 2× / Min / Max, auto cash-out switch + input. The form locks once a bet is in the round. **One big button** (`betAction()`, unit-tested): `login` → `place` "Place bet" / `closed` "Betting opens soon" → `waiting` "Waiting for the round…" → `cashout` "Cash out 13.50 VLX" (success, live) → result block `won` / `lost` / `refunded`.
- Live bets: compact table (avatar, nickname, bet, at, payout); my nickname in `--primary-text`; wins on a green tint, losses dimmed.
- Top strip: "Last 20" multiplier pills, each a link to its fairness check; skeleton pills while loading.
- Stage header: round number, hash badge (copy), live dot + online count.
- **Layout:**

| Width | Layout |
|---|---|
| ≥ 1280px | Stage + right column (380px): bet panel above live bets |
| 1024–1279px | Stage full width; bet panel and live bets side by side below |
| 768–1023px | Everything stacked |
| < 768px | Bet panel = **bottom sheet** above the bottom nav: folded shows a one-line summary ("Bet 10.00 VLX · Edit") + the big button; the handle unfolds the form. Chart height 42vh; extra bottom padding so the sheet never covers content |

---

## 9b. Panels, statistics and responsible play (Phase 4)

**Chart colors carry meaning, the same on every chart:**

| Tone | Means |
|---|---|
| `success` | cashed out, paid out (good for the player) |
| `danger` | lost bets |
| `primary` | volume and people (staked VLX, active players, cash-out counts) |
| `accent` | rounds and the net result line |
| `muted` | theory (the share the provably fair formula predicts) |

| Page | Pattern |
|---|---|
| Leaderboard | Two `vx-tabs` (period, ranking), kept in the URL. Podium for the top 3 (winner in the middle on wide screens, first on phones; #1 crown in warning tint, #2 neutral, #3 accent), then a table. My row: `ring-primary/40` on the podium, `bg-primary/5` + "You" badge in the table. Profit is green / red |
| Profile statistics | 4 stat cards (win rate, average cash-out, net result, total staked); "Results per day" (stacked cashed out / lost bars + net result line on the right axis, 7/30/90-day tabs), win-rate doughnut with the percent in the middle, cash-out ranges bar chart. Players only |
| Responsible play | Daily limit card with today's usage bar and the pending-increase notice (clock icon, warning); session reminder select; self-exclusion as three radio cards (a shorter option than the active pause is disabled) + a danger button behind `ModalService.confirm` that states the end time and that it cannot be undone; "Good to know" facts. An active pause shows a warning banner at the top |
| Crash page | A self-excluded player's big button becomes "Betting paused until …", a player whose daily limit is used up "Daily limit reached until …" (both secondary, pause icon, link to Responsible play). They are explanations, not actions, so their text may wrap (`h-auto min-h-14 whitespace-normal`): Georgian dates are long |
| Nothing invites a player who cannot bet (v1.1) | `LimitsStore.canPlayNow()` (from the server) false = self-excluded or daily limit used up. Then: game cards show a secondary **Watch** instead of the primary **Play**; no "Play now" (home hero, empty leaderboard), "Play" (empty history) or "Play a round" (empty statistics) buttons, and the empty-state texts drop their "play your first round" / "be the first" part; the session reminder's primary button says "Close" instead of "Keep playing". Navigation ("Play" item) and "Open the game" stay: watching is allowed |
| Session reminder | Modal "Time for a break?" (hourglass, warning tint): minutes played, bets, staked, and the reminder that Crash has no winning strategy. Buttons: "Review my limits" (ghost), "Keep playing" (primary) |
| Operator | Tabs "Live round" / "Games". Live: round card (state badge, big mono multiplier or countdown, hash badge, danger "Cancel round" → dialog with a required reason), 4 stat cards, live bets, last rounds as multiplier pills. Games: table with an inline status select (non-Active asks for confirmation), edit / new game dialogs |
| Home | Hero card (eyebrow "VELTRIX · CRASH", one headline, two lines of text, Play now / Create an account / How it works, three fact tiles), then the live round card (same real-time store as the Crash page: state badge, big mono multiplier or countdown, bets / staked / online, last-20 strip, "Open the game") beside "Top today" (5 rows: rank, avatar, nickname link, multiplier pill), then up to 3 game cards |
| Games catalog | Search (300ms debounce) + type + status selects, kept in the URL; responsive grid of game cards (icon tile, name, type, status badge, 2-line description, bet range, betting time, Play = primary, or a secondary "Watch" for a game in maintenance). Staff also see Disabled games |
| Player profile | Public data only: avatar xl, nickname (+ "You" badge and "Edit my profile" for oneself), level and member since; a Level card (`vx-level-progress`) beside an Achievements card (badges as accent pills: icon + title, unlock date in the tooltip); 4 stat cards, best cash-outs table with multiplier pills. Unknown nickname = empty state (the request is silent, no error toast) |
| Level and achievements (Phase 5, reworked in v1.1) | Accent is the progress color (XP is a reward, not a balance). `vx-level-progress`: "Level 3 · Climber", XP on the right in mono, accent bar from the current level's XP to the next, "20 XP to level 4 · Pilot" (or "Top level reached"). Profile → "Level and achievements" (players only): the level card, then the "Achievements" card ("Learn the game and its safety tools…") with a grid of task tiles (1 / 2 / 3 columns), safety first, then learning, then playing. Each tile: icon circle, title + a kind badge (Safety = success, Learning = primary, Playing = neutral), description, "+60 XP · +20 VLX" in mono accent, then "Unlocked {date}" in success, "Not unlocked yet" in muted, or "Unlocks when your next bet is settled" in accent (a goal met before achievements existed). Unlocked = accent border and tint, locked = neutral border, muted icon. No progress bars: every task is one step. When the server leaves out the play tasks, a muted note with a pause icon says why ("…hidden while your betting is paused or your daily limit is used up"). The icon NAME comes from the database, the picture from `achievementIcon()`: rocket, coins (drawn as the VLX gem: VLX is not money), timer, book, shield, limit (hand-heart). Bell: AchievementUnlocked (medal) and LevelUp (trophy) in accent, linking to the profile; their toast is the message ("Try Crash: +10 XP, +10 VLX."), not the generic title |
| Rules | The two content pages from the database in cards (`vx-markdown`), a side column linking Provably fair and Responsible play, and the virtual-points note |
| Settings | Two cards: Appearance (theme System / Dark / Light as `vx-tabs`, Animations switch) and Sound (Game sounds switch + three preview buttons). Every change applies at once and is saved on the account; a status line says "Saving…" |
| Admin | Section nav (Dashboard, Users, Audit log, Settings) under the page header. Dashboard: 8 stat cards + charts. Users: search (300ms debounce) + role / status selects, row opens the user drawer (role, block, balance adjustment with Add/Remove tabs and a confirm). Audit log: filters in the URL, a readable change summary ("Role: Player → Operator"), rows expand to the before / after JSON |
| AI assistant (players only) | Top bar: a ghost icon button (`icons.sparkles`) before the bell, only for players; it loads `features/assistant/assistant-drawer.ts` on the first click (the drawer, its store and the CDK Dialog are not in the first download). Drawer (`vx-drawer`, 440px, full width on phones): title "AI assistant". Empty chat: sparkles circle (primary tint), "Hi, {name}!", one line on what it explains, an info note that it gives no betting tips, and four suggested questions as outline buttons (none about winning). Conversation (`role="log"`, `aria-live="polite"`): the player's messages right, `bg-primary`, rounded with a smaller bottom-right corner; answers left with a small sparkles avatar, `bg-surface-2` + border, drawn with `vx-markdown` (text color, tighter gap). While waiting: the question at once (80% primary) and a spinner + "Thinking…". Footer: textarea (Enter sends, Shift+Enter new line, 500 characters, counter from 400), primary icon button Send, the note "Answers are written by AI and can be wrong." and a ghost "New chat" (trash icon, confirm dialog, danger). If the AI cannot answer: a danger note right above the box with the reason (not a toast: the answer is expected in the drawer), and the question goes back into the box. If no AI is set up (`GET /api/assistant/status`): a warning note "The assistant is not configured. An administrator has to connect it first." above the box, and the box, Send and the suggestions are disabled |

---

## 10. Page review checklist

After every page, check at **1440px, 768px and 375px**, in **both themes**:

- No horizontal page scroll (tables scroll inside their card).
- Sidebar ↔ bottom nav switch at 768px; nothing is hidden behind the bottom nav.
- Long labels and large numbers wrap or truncate instead of overflowing.
- Loading = skeletons, empty = empty state, errors = toast + field messages.
- Keyboard: Tab order is logical, focus is visible, Escape closes overlays.
- prefers-reduced-motion: no shimmer, shake or counting.

---

## 11. Decision log

| Date | Decision |
|---|---|
| 2026-10-25 | Design system adopted: tokens above, Tailwind v4, Angular CDK, Lucide, Inter + JetBrains Mono |
| 2026-10-25 | Fonts bundled from npm instead of the Google Fonts CDN (offline exam room) |
| 2026-10-25 | AA-derived text tokens (§2.2); danger buttons use the soft style; the light theme uses darker shades of accent/success/danger/warning |
| 2026-10-25 | Default Tailwind palette disabled; only token colors exist |
| 2026-10-25 | `vx-card` / `vx-stat-card` include `min-w-0` (prevents phone overflow inside grids) |
| 2026-10-25 | A temporary **legacy layer** at the end of `styles.css` keeps pages built before the design system working; each page drops it when rebuilt, then the layer is deleted |
| 2026-10-25 | All pages rebuilt with the design system; **legacy layer deleted** (and the old `FieldError` / pagination) |
| 2026-10-25 | New components: page header, tabs, pagination, password input, `.vx-switch`, `.vx-table-compact` |
| 2026-10-25 | VLX icon: gem instead of coins |
| 2026-10-25 | Top bar title is visual only; the page's `vx-page-header` owns the `<h1>` (one h1 per page) |
| 2026-10-25 | Crash page: two columns only from 1280px (at 1024px the side column was too narrow for the live bets table) |
| 2026-10-27 | New components: `vx-chart` (Chart.js, lazy-loaded), `vx-progress-bar`, drawer (`ModalService.openDrawer` + `vx-drawer`); Tabs get a `label` input |
| 2026-10-27 | Navigation by role: "Staff" group in the sidebar; bottom nav = Play, Top, History, Wallet, Profile; role pages in the account menu |
| 2026-10-27 | Bell: unread badge + notifications panel; chart tone meanings fixed (§9b) |
| 2026-10-27 | Chart lines use monotone interpolation (a smooth curve invented peaks above the real values) |
| 2026-10-27 | Card header wraps its actions under the title on phones |
| 2026-10-27 | Rules: `max-sm:hidden` for components with their own display; truncate inside table cells on an inner block; eager code imports single `shared/ui` files, not the barrel |
| 2026-10-27 | Spec coverage pass: Home, Games catalog, Player profile, Rules and Settings pages (§9b); `vx-markdown`; Home / Games / Rules in the navigation, Settings in the account menu, logo → Home |
| 2026-10-27 | Settings: theme System / Dark / Light (System follows the device live), "Animations off" = reduced motion (`data-motion`), game sounds for the player's own actions only; all saved on the account |
| 2026-10-28 | Top-bar dropdowns (account menu, notifications) load on demand: the initial download went from 499 kB to 405 kB with the same look and keyboard behavior |
| 2026-10-28 | Georgian and English (§8b): Georgian default, Noto Sans Georgian for Georgian letters, language button for guests, Settings → Language, all Georgian in `ka.json`; the bonus text button only from 1024px; big countdowns show a small unit |
| 2026-10-29 | Achievements and XP levels (§9b): `vx-level-progress`, achievement tiles on the profile, badges on the public profile, accent = progress; the bell's "Mark all read" never shrinks (Georgian label shortened to "ყველა წავიკითხე") |
| 2026-10-30 | v1.1 responsible play (§9b): task tiles get a Safety / Learning / Playing badge and drop progress bars; a note when play tasks are hidden; a player who cannot bet sees Watch instead of Play, no Play now / Play a round buttons, "Close" instead of "Keep playing", and a "Daily limit reached until …" bet button; the two explanation buttons may wrap (Georgian "…-მდე" dates overflowed the panel) |
| 2026-10-30 | Georgian wording pass (the author's list): the bell header wraps when the title and "Mark all read" do not fit on one line (the button moves under the title, aligned right; English stays on one row), so the full label "ყველას წაკითხულად მონიშვნა" is back; admin Settings buttons stack full width on phones (Save on top) because "ცვლილებების გაუქმება" + "პარამეტრების შენახვა" do not fit side by side; in one table two columns never share a Georgian title ("Result" = სტატუსი, "Profit" = შედეგი) |
| 2026-10-30 | AI assistant (§9b): players only, ✨ button in the top bar loading the drawer on demand; chat bubbles (player right in primary, assistant left on surface-2 with `vx-markdown`); new icons sparkles, send, trash |
| 2026-11-15 | Toasts above the CDK overlay (`z-[1100]`): with the same z-index 1000 an open drawer or dialog covered them, so the AI assistant "did nothing" when it failed. The assistant shows its errors and the "not configured" state as notes inside the drawer, above the box |

# VELTRIX design system

The single source of truth for every UI decision. **Any change to a design decision updates this file in the same commit.**

The look: a premium, modern fintech/gaming dashboard, not a template. Calm surfaces, one accent, numbers that are easy to read.

Implementation: tokens and base styles in [`Veltrix.Web/src/styles.css`](../Veltrix.Web/src/styles.css), and components in [`Veltrix.Web/src/app/shared/ui/`](../Veltrix.Web/src/app/shared/ui/). The living reference is the `/styleguide` route (development builds only), which shows every component in both themes.

---

## 1. Stack

| Concern | Choice |
|---|---|
| Styling | **Tailwind CSS v4**, configured in CSS (`@theme` in `styles.css`), loaded through PostCSS (`.postcssrc.json`) |
| Overlays | **Angular CDK**: Dialog (modals), Menu (account menu), Overlay (notifications panel) |
| Icons | **lucide-angular**. Only the icons listed in `shared/ui/icons.ts` are bundled |
| Fonts | **Inter** (UI) and **JetBrains Mono** (all numbers), the Google Fonts families served from npm (`@fontsource-variable/*`) so they also work offline |
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

Never: gradients on text, all-caps body text, more than two font families.

---

## 4. Layout

| Element | Rule |
|---|---|
| Content width | max **1440px**, centered |
| Page padding | **24px** desktop, **16px** below 768px |
| Breakpoint | **768px** (`md`) switches between phone and desktop layouts |
| Sidebar (≥768px) | fixed left, 240px expanded / 72px collapsed (icons only, with `title` tooltips), `bg-surface`, right border; state remembered in `localStorage` (`vx.sidebarCollapsed`); bottom: disclaimer + collapse button |
| Top bar | sticky, 64px, **glass**, bottom border. Left: page title (desktop, visual only: `aria-hidden`, the page's own `<h1>` is the real heading) or logo (phone). Right: balance chip (animated, gem icon) → daily bonus (button when claimable, countdown chip otherwise) → notifications bell → avatar menu. Guests: theme toggle, Log in, Register |
| Page header | every page starts with `<vx-page-header title description>`: the `<h1>` is visible on phones and visually hidden from 768px (the top bar shows the title), the description is always visible; actions go in `[page-actions]` |
| Bottom nav (<768px) | fixed, glass, 64px + safe-area inset, 5 items (icon + 11px label); active = `text-primary-text` |
| Page bottom padding on phones | enough for the bottom nav (`pb-28`) |
| Bet panel on phones | becomes a **bottom sheet** (Crash page, step 3) |

The navigation items are defined once in `layout/nav-items.ts`, for both the sidebar and the bottom nav.

**Grid/flex children that contain tables or long content need `min-w-0`**; otherwise they grow wider than the screen. `vx-card` and `vx-stat-card` already include it.

---

## 5. Components (`shared/ui`)

Import from the barrel: `import { Button, Card } from '../../shared/ui';`

| Component | Selector / usage | Notes |
|---|---|---|
| Button | `<button vxButton variant size [loading] [disabled] [block]>` (also on `<a>`) | Styles the **native** element (keeps keyboard and form behavior). Variants: `primary`, `secondary`, `outline`, `ghost`, `success`, `danger` (soft: red text on a red tint, for AA). Sizes: `sm` 32px, `md` 40px, `lg` 48px, `xl` 56px, `icon` 40px, `icon-sm` 32px. `loading` shows a spinner and disables |
| Card | `<vx-card title subtitle padding="default\|none">`, actions in `[card-actions]` | `padding="none"` for edge-to-edge tables |
| Field | `<vx-field label for hint [control] patternMessage>` + projected `<input class="vx-input">` | Shows the first error once touched (same messages as `controlErrorMessage`). Uses Default change detection on purpose (form state is not a signal) |
| Input classes | `.vx-input`, `select.vx-input`, `.vx-input-wrap` + `.vx-input-icon` / `.vx-input-suffix`, `.vx-checkbox`, `.vx-label` | Focus: primary border + 4px soft ring. Invalid + touched: danger border |
| Table | `<div class="vx-table-wrap"><table class="vx-table">`; add `vx-table-compact` in narrow columns | Sticky uppercase header, row hover, `text-right` + `num` for numeric columns. On phones, hide secondary columns (`hidden md:table-cell`) instead of forcing horizontal scroll |
| Switch | `<input type="checkbox" class="vx-switch">` | On/off toggle that keeps native checkbox semantics (keyboard, forms) |
| Password input | `<vx-password-input><input class="vx-input" type="password" …></vx-password-input>` | Adds a show/hide button; the projected input keeps `formControlName` and `autocomplete` |
| Page header | `<vx-page-header title description>` + `[page-actions]` | See §4 |
| Tabs | `<vx-tabs [items] [(value)]>` | Segmented control, `role="tablist"`, ← → keys move between tabs |
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
| Theme toggle | `<vx-theme-toggle>` | Sun / moon icon button |
| Modal | `ModalService.open(Component, { data, ariaLabel, width })`, `ModalService.confirm({ title, message, confirmText, tone })`; dialogs wrap content in `<vx-modal title description>` with `[modal-footer]` | CDK Dialog: focus trap, Escape, restores focus. Glass panel, blurred backdrop |
| Toasts | `ToastStore.success / error / info(message)` (the API error interceptor calls `error`) | Top on phones, bottom-right on desktop; icon per kind; errors use `role="alert"` and stay longer (6s vs 3.5s) |
| Icons | `<lucide-icon [img]="icons.wallet" [size]="18" />` with `protected readonly icons = Icons` | Add new icons to `icons.ts` only. Sizes: 14 (dense), 16 (buttons, menus), 18 (nav, icon buttons), 20–22 (empty states, bottom nav). **VLX amounts use `icons.vlx` (a gem)**: a coin would suggest money, and Lucide's coins icon reads like a chain link at 16px |

Rules:
- Use these components on new and rebuilt pages. Don't create one-off styled buttons, cards or inputs.
- A new reusable visual element becomes a component in `shared/ui`, is added to `/styleguide`, and is documented here.

---

## 6. Glass, glow, gradients

- **Glass** (`glass` utility: 70% surface, `backdrop-blur`) **only** on the top bar, the bottom nav and modals.
- **Glow**: one accent glow, **only on the live multiplier** while the round is running (`[text-shadow:var(--vx-glow)]`).
- **Gradients**: only the soft fill under the multiplier curve and the skeleton shimmer. No gradient text, no neon.

---

## 7. Motion

| Rule | Value |
|---|---|
| Hover / focus / state transitions | **150–250ms, ease-out** (`duration-200 ease-out-soft`, i.e. `cubic-bezier(0.22, 1, 0.36, 1)`) |
| Overlays appearing | `animate-pop-in` (menus, modals, 200ms), `animate-slide-up` (toasts, 250ms) |
| Balance changes | animated number (650ms) |
| Crash | multiplier turns `--danger` + `animate-shake` (450ms, once) |
| Loading | skeleton shimmer, no spinners (the button's own loading spinner is the exception) |
| **prefers-reduced-motion** | all animations and transitions become instant; the animated number jumps; no shimmer and no shake |

---

## 8. Accessibility (WCAG AA)

- Text contrast ≥ 4.5:1 (large text ≥ 3:1); non-text graphics ≥ 3:1 (see §2.2).
- **Visible focus everywhere**: global `:focus-visible` outline (2px `--primary-text`, 2px offset); inputs also get the soft ring.
- Icon-only buttons always have an `aria-label`; decorative icons `aria-hidden="true"`.
- "Skip to content" link as the first focusable element.
- Menus and dialogs use the CDK (keyboard navigation, focus trap, Escape).
- Errors: `role="alert"`; live values that change constantly (the multiplier) are **not** announced on every tick.

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

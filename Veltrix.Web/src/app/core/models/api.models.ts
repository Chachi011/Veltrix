// TypeScript mirrors of the backend DTOs. Enums travel as strings (JsonStringEnumConverter on the server).
// Dates are ISO-8601 UTC strings ("2026-10-24T17:21:48Z").

export type UserRole = 'Player' | 'Operator' | 'Admin';
export type GameStatus = 'Active' | 'Maintenance' | 'Disabled';
export type RoundState =
  | 'Waiting'
  | 'BettingOpen'
  | 'BettingClosed'
  | 'Running'
  | 'Crashed'
  | 'Finished'
  | 'Cancelled';
export type BetStatus = 'Active' | 'CashedOut' | 'Lost' | 'Refunded';
export type TransactionType = 'Bet' | 'Win' | 'Refund' | 'Bonus' | 'AdminAdjustment';

/** RFC 7807 error body returned by every failing endpoint. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  /** Validation errors by camelCase field name. */
  errors?: Record<string, string[]>;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ---------- Auth & profile ----------

export interface UserDto {
  id: number;
  email: string;
  nickname: string;
  role: UserRole;
  avatarUrl: string | null;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  nickname: string;
  password: string;
  dateOfBirth: string; // yyyy-MM-dd
  isAdultConfirmed: boolean;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
}

export interface ProfileDto {
  id: number;
  email: string;
  nickname: string;
  role: UserRole;
  avatarUrl: string | null;
  dateOfBirth: string;
  createdAt: string;
  balance: number;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface BestCashOutDto {
  multiplier: number;
  payout: number;
  cashedOutAt: string;
}

export interface PublicProfileDto {
  nickname: string;
  avatarUrl: string | null;
  memberSince: string;
  level: number;
  levelTitle: string;
  totalBets: number;
  cashOutCount: number;
  biggestMultiplier: number;
  biggestWin: number;
  bestCashOuts: BestCashOutDto[];
}

// ---------- Games & rounds ----------

export interface GameDto {
  id: number;
  name: string;
  slug: string;
  description: string | null;
  type: 'Crash';
  status: GameStatus;
  minBet: number;
  maxBet: number;
  bettingDurationSec: number;
  pauseBetweenRoundsSec: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface RoundDto {
  id: number;
  gameSlug: string;
  roundNumber: number;
  state: RoundState;
  serverSeedHash: string;
  /** Null until the round is over. */
  serverSeed: string | null;
  /** Null until the round is over. */
  crashPoint: number | null;
  currentMultiplier: number | null;
  createdAt: string;
  bettingEndsAt: string | null;
  startedAt: string | null;
  crashedAt: string | null;
  endedAt: string | null;
}

export interface RoundBetDto {
  betId: number;
  nickname: string;
  avatarUrl: string | null;
  amount: number;
  slot: number;
  status: BetStatus;
  cashOutMultiplier: number | null;
  payout: number | null;
  placedAt: string;
}

// ---------- Bets ----------

export interface PlaceBetRequest {
  gameSlug: string;
  amount: number;
  autoCashOutAt: number | null;
  slot: number;
}

export interface BetDto {
  id: number;
  roundId: number;
  roundNumber: number;
  gameSlug: string;
  amount: number;
  slot: number;
  autoCashOutAt: number | null;
  status: BetStatus;
  cashOutMultiplier: number | null;
  payout: number | null;
  crashPoint: number | null;
  placedAt: string;
  cashedOutAt: string | null;
}

export interface BetResultDto {
  bet: BetDto;
  balance: number;
}

export interface MyBetsQuery {
  page: number;
  pageSize: number;
  status?: BetStatus | null;
  from?: string | null;
  to?: string | null;
}

// ---------- Wallet ----------

export interface WalletDto {
  balance: number;
  dailyBonusAmount: number;
  canClaimDailyBonus: boolean;
  lastDailyBonusAt: string | null;
  nextDailyBonusAt: string | null;
}

export interface WalletTransactionDto {
  id: number;
  type: TransactionType;
  amount: number;
  balanceBefore: number;
  balanceAfter: number;
  betId: number | null;
  reason: string | null;
  createdAt: string;
}

export interface TransactionQuery {
  page: number;
  pageSize: number;
  type?: TransactionType | null;
}

// ---------- Fairness ----------

export interface FairnessRoundDto {
  roundId: number;
  roundNumber: number;
  state: RoundState;
  serverSeedHash: string;
  serverSeed: string | null;
  crashPoint: number | null;
  isVerified: boolean | null;
  algorithm: string;
}

export interface VerifyFairnessRequest {
  serverSeed: string;
  serverSeedHash: string | null;
  crashPoint: number | null;
}

export interface VerifyFairnessResult {
  computedHash: string;
  computedCrashPoint: number;
  hashMatches: boolean | null;
  crashPointMatches: boolean | null;
  algorithm: string;
}

// ---------- SignalR events (/hubs/crash) ----------

export interface RoundStateChangedEvent {
  roundId: number;
  roundNumber: number;
  state: RoundState;
  serverSeedHash: string;
  bettingEndsAt: string | null;
  startedAt: string | null;
  /** The server's clock when the event was sent (corrects a wrong local clock). */
  serverTime: string;
}

export interface MultiplierTickEvent {
  roundId: number;
  multiplier: number;
  elapsedMs: number;
}

export interface RoundCrashedEvent {
  roundId: number;
  crashPoint: number;
  serverSeed: string;
}

export interface BetPlacedEvent {
  roundId: number;
  nickname: string;
  amount: number;
}

export interface PlayerCashedOutEvent {
  roundId: number;
  nickname: string;
  multiplier: number;
  payout: number;
}

export interface BalanceUpdatedEvent {
  balance: number;
}

export interface OnlineCountEvent {
  count: number;
}

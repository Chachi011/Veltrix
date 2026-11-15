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
  /** Experience points (from settled bets and achievements). */
  xp: number;
  /** XP the current level starts at. */
  levelXp: number;
  /** The next level; null at the top level. */
  nextLevel: number | null;
  nextLevelTitle: string | null;
  nextLevelXp: number | null;
  /** Unlocked achievements, newest first. */
  badges: BadgeDto[];
}

// ---------- Achievements (Phase 5) ----------

/** Play needs a bet (hidden while the player cannot bet); Learn and Safety never do. */
export type AchievementKind = 'Play' | 'Learn' | 'Safety';

export interface AchievementDto {
  code: string;
  /** English title and description from the database seed (shown with | serverText). */
  title: string;
  description: string;
  /** Icon name chosen by the client: rocket, coins, timer, book, shield, limit. */
  icon: string;
  xpReward: number;
  vlxReward: number;
  /** How many players unlocked it. */
  unlockedBy: number;
  kind: AchievementKind;
}

export interface MyAchievementDto extends AchievementDto {
  unlockedAt: string | null;
  /** 0 or 1 of 1 for first bet / first cash-out; null for the others. */
  progress: number | null;
  target: number | null;
}

export interface MyAchievementsDto {
  items: MyAchievementDto[];
  /** True when tasks that ask to play were left out: the player is self-excluded or used up today's limit. */
  playTasksHidden: boolean;
}

export interface BadgeDto {
  code: string;
  title: string;
  icon: string;
  unlockedAt: string;
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

export interface NotificationDto {
  id: number;
  type: NotificationType;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

// ---------- Leaderboard & statistics (Phase 4) ----------

export type LeaderboardPeriod = 'Day' | 'Week' | 'All';
export type LeaderboardType = 'Multiplier' | 'Profit';

export interface LeaderboardEntryDto {
  rank: number;
  nickname: string;
  avatarUrl: string | null;
  level: number;
  /** The multiplier (e.g. 12.34) or the profit in VLX, depending on the type. */
  value: number;
  betsCount: number;
}

export interface LeaderboardDto {
  period: LeaderboardPeriod;
  type: LeaderboardType;
  from: string | null;
  generatedAt: string;
  entries: LeaderboardEntryDto[];
}

export interface DailyPointDto {
  date: string; // yyyy-MM-dd
  bets: number;
  cashOuts: number;
  wagered: number;
  payout: number;
  profit: number;
}

export interface MultiplierBucketDto {
  label: string;
  from: number;
  to: number | null;
  count: number;
  percent: number;
  /** Share predicted by the provably fair formula (crash points only). */
  expectedPercent: number | null;
}

export interface MyStatsDto {
  totalBets: number;
  cashOutCount: number;
  lostCount: number;
  winRate: number;
  totalWagered: number;
  totalPayout: number;
  netProfit: number;
  averageBet: number;
  averageCashOut: number;
  biggestMultiplier: number;
  biggestWin: number;
  daily: DailyPointDto[];
  cashOutDistribution: MultiplierBucketDto[];
}

// ---------- Responsible play (Phase 4) ----------

export type SelfExclusionPeriod = 'Hours24' | 'Days7' | 'Days30';

export interface PlayerLimitsDto {
  dailyBetLimit: number | null;
  hasPendingLimitChange: boolean;
  /** Null together with hasPendingLimitChange = the limit will be removed. */
  pendingDailyBetLimit: number | null;
  pendingLimitAppliesAt: string | null;
  betToday: number;
  remainingToday: number | null;
  dayResetsAt: string;
  /** 0 = off. */
  sessionReminderMin: number;
  isSelfExcluded: boolean;
  selfExcludedUntil: string | null;
  limitIncreaseDelayHours: number;
  /** False while self-excluded or when today's limit is used up: then nothing may invite the player to play. */
  canPlayNow: boolean;
}

export interface UpdateLimitsRequest {
  dailyBetLimit: number | null;
  sessionReminderMin: number;
}

// ---------- Notifications (Phase 4) ----------

export type NotificationType =
  | 'DailyBonus'
  | 'BalanceAdjusted'
  | 'RoleChanged'
  | 'AccountBlocked'
  | 'AccountUnblocked'
  | 'PasswordChanged'
  | 'SelfExcluded'
  | 'AchievementUnlocked'
  | 'LevelUp';

export interface NotificationPageDto extends PagedResult<NotificationDto> {
  unreadCount: number;
}

// ---------- Operator (Phase 4) ----------

export interface SaveGameRequest {
  name: string;
  slug: string;
  description: string | null;
  minBet: number;
  maxBet: number;
  bettingDurationSec: number;
  pauseBetweenRoundsSec: number;
}

export interface RoundCancellationDto {
  roundId: number;
  refundedBetsCount: number;
  refundedTotal: number;
  reason: string;
  cancelledAt: string;
}

// ---------- Admin (Phase 4) ----------

export interface AdminUserQuery {
  page: number;
  pageSize: number;
  search?: string | null;
  role?: UserRole | null;
  isBlocked?: boolean | null;
}

export interface AdminUserListItemDto {
  id: number;
  email: string;
  nickname: string;
  avatarUrl: string | null;
  role: UserRole;
  isBlocked: boolean;
  balance: number;
  totalBets: number;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface BalanceAdjustmentDto {
  id: number;
  userId: number;
  amount: number;
  reason: string;
  adminNickname: string;
  balanceBefore: number;
  balanceAfter: number;
  createdAt: string;
}

export interface AdminUserDetailsDto {
  id: number;
  email: string;
  nickname: string;
  avatarUrl: string | null;
  role: UserRole;
  dateOfBirth: string;
  isBlocked: boolean;
  isEmailVerified: boolean;
  failedLoginCount: number;
  lockoutUntil: string | null;
  createdAt: string;
  lastLoginAt: string | null;
  activeSessions: number;
  balance: number;
  totalBets: number;
  cashOutCount: number;
  totalWagered: number;
  totalPayout: number;
  biggestMultiplier: number;
  dailyBetLimit: number | null;
  selfExcludedUntil: string | null;
  recentTransactions: WalletTransactionDto[];
  recentAdjustments: BalanceAdjustmentDto[];
}

export interface BalanceAdjustmentRequest {
  amount: number;
  reason: string;
  /** One per dialog: a double-click or a retry cannot apply the same adjustment twice. */
  requestId: string;
}

export interface AuditLogQuery {
  page: number;
  pageSize: number;
  action?: string | null;
  entityType?: string | null;
  entityId?: string | null;
}

export interface AuditLogDto {
  id: number;
  actorUserId: number | null;
  actorNickname: string;
  action: string;
  entityType: string;
  entityId: string;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}

export interface SystemSettingsDto {
  startingBalance: number;
  dailyBonusAmount: number;
  updatedAt: string;
}

export interface AdminOverviewDto {
  totalUsers: number;
  playersCount: number;
  blockedUsers: number;
  newUsersToday: number;
  onlineNow: number;
  activePlayersToday: number;
  roundsToday: number;
  betsToday: number;
  betsVolumeToday: number;
  payoutToday: number;
  houseResultToday: number;
  vlxInCirculation: number;
  roundsSampled: number;
  averageCrashPoint: number;
  crashPointDistribution: MultiplierBucketDto[];
}

export interface DailyStatisticDto {
  date: string; // yyyy-MM-dd
  activeUsers: number;
  newUsers: number;
  roundsPlayed: number;
  betsCount: number;
  betsVolume: number;
  payoutVolume: number;
  averageCrashPoint: number;
}

// ---------- Settings, content, catalog (spec coverage pass) ----------

/** System = follow the device setting. */
export type ThemePreference = 'System' | 'Dark' | 'Light';

/** UI language: Georgian (default) or English. */
export type LanguageCode = 'ka' | 'en';

export interface UserPreferencesDto {
  theme: ThemePreference;
  soundEnabled: boolean;
  /** False = reduce motion. */
  animationsEnabled: boolean;
  language: LanguageCode;
  updatedAt: string;
}

export interface UpdatePreferencesRequest {
  theme: ThemePreference;
  soundEnabled: boolean;
  animationsEnabled: boolean;
  language: LanguageCode;
}

export interface StaticPageDto {
  slug: string;
  language: string;
  title: string;
  /** Markdown subset: paragraphs, lists, **bold**, *italic*. */
  content: string;
  updatedAt: string;
}

export interface GameQuery {
  search?: string | null;
  type?: 'Crash' | null;
  status?: GameStatus | null;
}

// ---------- AI assistant (players only) ----------

export type AssistantMessageRole = 'User' | 'Assistant';

export interface AssistantMessageDto {
  id: number;
  role: AssistantMessageRole;
  /** The player's question as typed, or the answer: Markdown subset (paragraphs, lists, **bold**, *italic*). */
  text: string;
  createdAt: string;
}

/** The saved question and the assistant's answer to it. */
export interface AssistantReplyDto {
  question: AssistantMessageDto;
  answer: AssistantMessageDto;
}

/** GET /api/assistant/status: false until an admin connects the n8n workflow. */
export interface AssistantStatusDto {
  configured: boolean;
}

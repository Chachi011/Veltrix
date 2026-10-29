import { mark } from '../core/i18n/i18n';
import { BetStatus, GameStatus, RoundState, TransactionType, UserRole } from '../core/models/api.models';

// The English names of the API's enum values, shown through the t pipe: {{ betStatusLabels[bet.status] | t }}.
// One place, so every page names a status the same way (and the i18n check finds them).

export const BET_STATUS_LABELS: Record<BetStatus, string> = {
  Active: mark('Active'),
  CashedOut: mark('Cashed out'),
  Lost: mark('Lost'),
  Refunded: mark('Refunded')
};

export const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  Bet: mark('Bet'),
  Win: mark('Win'),
  Refund: mark('Refund'),
  Bonus: mark('Bonus'),
  AdminAdjustment: mark('Adjustment')
};

export const ROLE_LABELS: Record<UserRole, string> = {
  Player: mark('Player'),
  Operator: mark('Operator'),
  Admin: mark('Admin')
};

export const GAME_STATUS_LABELS: Record<GameStatus, string> = {
  Active: mark('Active'),
  Maintenance: mark('Maintenance'),
  Disabled: mark('Disabled')
};

export const ROUND_STATE_LABELS: Record<RoundState, string> = {
  Waiting: mark('Waiting'),
  BettingOpen: mark('Betting open'),
  BettingClosed: mark('Betting closed'),
  Running: mark('Running'),
  Crashed: mark('Crashed'),
  Finished: mark('Finished'),
  Cancelled: mark('Cancelled')
};

import { DomainError } from './DomainError';

/**
 * Raised client-side when a withdrawal/transfer is attempted for more than
 * the wallet's known balance, so the user gets instant feedback before a
 * round trip to the server (the server remains the source of truth).
 */
export class InsufficientFundsError extends DomainError {
  public constructor(walletName: string) {
    super(`"${walletName}" does not have enough balance to cover this amount.`);
  }
}

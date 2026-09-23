/**
 * Transaction kinds surfaced by the wallet ledger.
 * The API returns `type` as a free-text string, so `toTransactionType`
 * normalizes it defensively instead of trusting the raw value.
 */
export enum TransactionType {
  Deposit = 'Deposit',
  Withdrawal = 'Withdrawal',
  TransferIn = 'TransferIn',
  TransferOut = 'TransferOut',
  Unknown = 'Unknown',
}

export function toTransactionType(raw: string | null | undefined): TransactionType {
  const normalized = (raw ?? '').trim().toLowerCase();
  switch (normalized) {
    case 'deposit':
      return TransactionType.Deposit;
    case 'withdrawal':
    case 'withdraw':
      return TransactionType.Withdrawal;
    case 'transferin':
    case 'transfer_in':
      return TransactionType.TransferIn;
    case 'transferout':
    case 'transfer_out':
    case 'transfer':
      return TransactionType.TransferOut;
    default:
      return TransactionType.Unknown;
  }
}

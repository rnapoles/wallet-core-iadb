/** Wire shape for a wallet as returned by GET /api/wallets and GET /api/wallets/{id} (WalletDto). */
export interface WalletDTO {
  id: string;
  name: string | null;
  currency: string | null;
  balance: number;
  createdAt: string;
  lastTransactionAt: string | null;
}

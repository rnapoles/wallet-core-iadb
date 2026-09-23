/** Wire shape for POST /api/transactions/withdraw (WithdrawCommand). */
export interface WithdrawRequestDTO {
  walletId: string;
  amount: number;
  description: string | null;
  reference: string | null;
}

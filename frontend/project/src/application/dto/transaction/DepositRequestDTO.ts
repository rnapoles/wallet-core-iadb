/** Wire shape for POST /api/transactions/deposit (DepositCommand). */
export interface DepositRequestDTO {
  walletId: string;
  amount: number;
  description: string | null;
  reference: string | null;
}

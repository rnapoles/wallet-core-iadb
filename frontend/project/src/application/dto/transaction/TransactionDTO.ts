/** Wire shape for a ledger record (TransactionDto). */
export interface TransactionDTO {
  id: string;
  type: string | null;
  amount: number;
  description: string | null;
  walletId: string;
  relatedWalletId: string | null;
  reference: string | null;
  createdAt: string;
  isCompleted: boolean;
}

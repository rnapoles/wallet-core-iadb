/** Wire shape for POST /api/transactions/transfer (TransferCommand). */
export interface TransferRequestDTO {
  fromWalletId: string;
  toWalletId: string;
  amount: number;
  description: string | null;
  reference: string | null;
}

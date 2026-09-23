/** Wire shape returned by deposit/withdraw/transfer endpoints (TransactionResponse). */
export interface TransactionResponseDTO {
  transactionId: string;
  type: string | null;
  amount: number;
  newBalance: number;
  message: string | null;
}

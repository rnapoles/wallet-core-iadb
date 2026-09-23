/** Wire shape returned by POST /api/wallets (CreateWalletResponse). */
export interface CreateWalletResponseDTO {
  walletId: string;
  name: string | null;
  currency: string | null;
  balance: number;
  message: string | null;
}

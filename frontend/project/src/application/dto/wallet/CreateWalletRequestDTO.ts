/** Wire shape for POST /api/wallets (CreateWalletCommand). userId is injected server-side from the JWT in this UI's flows. */
export interface CreateWalletRequestDTO {
  userId: string;
  name: string;
  currency: string;
}

/** Wire shape for POST /api/auth/refresh (RefreshTokenCommand). */
export interface RefreshTokenRequestDTO {
  refreshToken: string;
  accessToken: string;
}

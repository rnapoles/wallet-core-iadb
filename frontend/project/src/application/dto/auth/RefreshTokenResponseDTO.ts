/** Wire shape returned by POST /api/auth/refresh (RefreshTokenResponse). */
export interface RefreshTokenResponseDTO {
  userId: string;
  email: string | null;
  newAccessToken: string | null;
  newRefreshToken: string | null;
  expiresAt: string;
}

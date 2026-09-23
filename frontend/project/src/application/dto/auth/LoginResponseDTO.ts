/** Wire shape returned by POST /api/auth/login (LoginResponse). */
export interface LoginResponseDTO {
  userId: string;
  email: string | null;
  token: string | null;
  refreshToken: string | null;
  expiresAt: string;
}

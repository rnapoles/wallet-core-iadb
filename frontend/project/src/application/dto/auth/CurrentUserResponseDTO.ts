/** Wire shape returned by GET /api/auth/me (GetCurrentUserResponse). */
export interface CurrentUserResponseDTO {
  userId: string;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
}

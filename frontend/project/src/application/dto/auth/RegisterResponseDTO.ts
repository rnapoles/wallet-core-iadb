/** Wire shape returned by POST /api/auth/register (RegisterUserResponse). */
export interface RegisterResponseDTO {
  userId: string;
  email: string | null;
  message: string | null;
}

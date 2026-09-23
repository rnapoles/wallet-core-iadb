/** Wire shape for POST /api/auth/register (RegisterUserCommand). */
export interface RegisterRequestDTO {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

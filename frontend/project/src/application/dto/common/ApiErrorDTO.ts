/** Normalized shape for a failed API response, regardless of the backend's error envelope. */
export interface ApiErrorDTO {
  status: number;
  message: string;
  requestId?: string;
}

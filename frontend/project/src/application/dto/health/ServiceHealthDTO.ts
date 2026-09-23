/** Wire shape for a single dependency's health (ServiceHealth). */
export interface ServiceHealthDTO {
  name: string | null;
  status: number;
  description: string | null;
  responseTime: string | null;
  checkedAt: string;
}

/** Central route path constants for the app router. */
export const routes = {
  login: '/login',
  register: '/register',
  dashboard: '/',
  walletDetail: (id: string): string => `/wallets/${id}`,
  walletDetailPattern: '/wallets/:walletId',
} as const;

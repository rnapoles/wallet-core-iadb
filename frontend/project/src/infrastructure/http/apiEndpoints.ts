/**
 * Centralized, semantic endpoint paths matching the OpenAPI spec exactly.
 * Keeping these in one place means a path never gets hand-typed (and
 * potentially mistyped) at multiple call sites.
 */
export const apiEndpoints = {
  auth: {
    register: '/auth/register',
    login: '/auth/login',
    me: '/auth/me',
    refresh: '/auth/refresh',
  },
  health: {
    check: '/health',
    live: '/health/live',
    ready: '/health/ready',
  },
  wallets: {
    collection: '/wallets',
    byId: (id: string): string => `/wallets/${encodeURIComponent(id)}`,
  },
  transactions: {
    deposit: '/transactions/deposit',
    withdraw: '/transactions/withdraw',
    transfer: '/transactions/transfer',
    byId: (id: string): string => `/transactions/${encodeURIComponent(id)}`,
    byWallet: (walletId: string): string => `/transactions/wallet/${encodeURIComponent(walletId)}`,
  },
} as const;

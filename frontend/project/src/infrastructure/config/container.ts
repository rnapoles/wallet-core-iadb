import { httpClient } from '../http/HttpClient';
import { TokenRefreshScheduler } from '../auth/TokenRefreshScheduler';
import { AuthRepository } from '../repositories/AuthRepository';
import { WalletRepository } from '../repositories/WalletRepository';
import { TransactionRepository } from '../repositories/TransactionRepository';
import { HealthRepository } from '../repositories/HealthRepository';
import { AuthService } from '../../application/services/AuthService';
import { WalletService } from '../../application/services/WalletService';
import { TransactionService } from '../../application/services/TransactionService';
import { HealthService } from '../../application/services/HealthService';

/**
 * Composition root. This is the single place in the codebase where a
 * concrete infrastructure class is bound to a domain port and handed to an
 * application service — every other module depends on interfaces/services,
 * never on `axios` or a `*Repository` class directly (Dependency Inversion).
 */
const authRepository = new AuthRepository(httpClient);
const walletRepository = new WalletRepository(httpClient);
const transactionRepository = new TransactionRepository(httpClient);
const healthRepository = new HealthRepository(httpClient);

/** Proactively refreshes the access token; started on login, stopped on logout — see AuthProvider. */
export const tokenRefreshScheduler = new TokenRefreshScheduler(httpClient);

export const container = {
  authService: new AuthService(authRepository),
  walletService: new WalletService(walletRepository),
  transactionService: new TransactionService(transactionRepository),
  healthService: new HealthService(healthRepository),
} as const;

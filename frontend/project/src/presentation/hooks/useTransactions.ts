import { useCallback, useEffect, useState } from 'react';
import type { Wallet } from '../../domain/entities/Wallet';
import type { Transaction } from '../../domain/entities/Transaction';
import { container } from '../../infrastructure/config/container';
import { getErrorMessage } from '../../shared/utils/getErrorMessage';

interface UseTransactionsResult {
  transactions: Transaction[];
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

/** Loads the transaction history for a single wallet. */
export function useTransactions(wallet: Wallet | null): UseTransactionsResult {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    if (!wallet) return;
    setError(null);
    setIsLoading(true);
    try {
      const result = await container.transactionService.listByWallet(wallet);
      setTransactions(result);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  }, [wallet]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return { transactions, isLoading, error, refresh };
}

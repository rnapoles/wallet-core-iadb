import { useCallback, useEffect, useState } from 'react';
import type { Wallet } from '../../domain/entities/Wallet';
import { container } from '../../infrastructure/config/container';
import { getErrorMessage } from '../../shared/utils/getErrorMessage';

interface UseWalletsResult {
  wallets: Wallet[];
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  addWalletOptimistically: (wallet: Wallet) => void;
}

/** Loads the authenticated user's wallets and exposes a refresh function for after mutations. */
export function useWallets(): UseWalletsResult {
  const [wallets, setWallets] = useState<Wallet[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setError(null);
    try {
      const result = await container.walletService.listWallets();
      setWallets(result);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const addWalletOptimistically = useCallback((wallet: Wallet) => {
    setWallets((current) => [wallet, ...current]);
  }, []);

  return { wallets, isLoading, error, refresh, addWalletOptimistically };
}

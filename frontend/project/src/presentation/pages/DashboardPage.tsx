import { useState } from 'react';
import { Plus } from 'lucide-react';
import { AppShell } from '../components/layout/AppShell';
import { WalletCard } from '../components/feature/wallet/WalletCard';
import { CreateWalletModal } from '../components/feature/wallet/CreateWalletModal';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';
import { useWallets } from '../hooks/useWallets';
import { useAuth } from '../hooks/useAuth';
import styles from './DashboardPage.module.css';

export function DashboardPage(): React.JSX.Element {
  const { user } = useAuth();
  const { wallets, isLoading, error, addWalletOptimistically } = useWallets();
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const firstName = user?.firstName ?? user?.displayName.split(' ')[0] ?? 'there';

  return (
    <AppShell>
      <header className={styles.header}>
        <div>
          <p className={styles.eyebrow}>{wallets.length} {wallets.length === 1 ? 'wallet' : 'wallets'}</p>
          <h1 className={styles.title}>Good to see you, {firstName}.</h1>
        </div>
        <Button onClick={() => setIsCreateOpen(true)}>
          <Plus size={16} />
          New wallet
        </Button>
      </header>

      {isLoading && (
        <div className={styles.centered}>
          <Spinner />
        </div>
      )}

      {!isLoading && error && <p className={styles.error}>{error}</p>}

      {!isLoading && !error && wallets.length === 0 && (
        <div className={styles.empty}>
          <p className={styles.emptyTitle}>No wallets yet</p>
          <p className={styles.emptyBody}>Open your first wallet to start tracking deposits and transfers.</p>
          <Button onClick={() => setIsCreateOpen(true)} style={{ marginTop: 'var(--space-4)' }}>
            <Plus size={16} />
            Create a wallet
          </Button>
        </div>
      )}

      {!isLoading && !error && wallets.length > 0 && (
        <div className={styles.grid}>
          {wallets.map((wallet) => (
            <WalletCard key={wallet.id.toString()} wallet={wallet} />
          ))}
        </div>
      )}

      <CreateWalletModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onCreated={addWalletOptimistically}
      />
    </AppShell>
  );
}

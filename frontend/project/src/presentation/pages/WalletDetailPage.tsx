import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowDownToLine, ArrowUpFromLine, ArrowLeftRight, ChevronLeft } from 'lucide-react';
import { AppShell } from '../components/layout/AppShell';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';
import { AnimatedNumber } from '../components/ui/AnimatedNumber';
import { TransactionList } from '../components/feature/transaction/TransactionList';
import { MoneyMovementModal } from '../components/feature/transaction/MoneyMovementModal';
import { useTransactions } from '../hooks/useTransactions';
import { container } from '../../infrastructure/config/container';
import { Money } from '../../domain/value-objects/Money';
import type { Wallet } from '../../domain/entities/Wallet';
import { getErrorMessage } from '../../shared/utils/getErrorMessage';
import { routes } from '../../shared/constants/routes';
import styles from './WalletDetailPage.module.css';

type MovementKind = 'deposit' | 'withdraw' | 'transfer';

export function WalletDetailPage(): React.JSX.Element {
  const { walletId } = useParams<{ walletId: string }>();
  const navigate = useNavigate();

  const [wallet, setWallet] = useState<Wallet | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeModal, setActiveModal] = useState<MovementKind | null>(null);

  const { transactions, isLoading: isTransactionsLoading, refresh: refreshTransactions } =
    useTransactions(wallet);

  async function loadWallet(): Promise<void> {
    if (!walletId) return;
    setIsLoading(true);
    setError(null);
    try {
      const result = await container.walletService.getWallet(walletId);
      setWallet(result);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadWallet();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [walletId]);

  async function handleCompleted(): Promise<void> {
    await loadWallet();
    await refreshTransactions();
  }

  return (
    <AppShell>
      <button
        type="button"
        className={styles.back}
        onClick={() => {
          void navigate(routes.dashboard);
        }}
      >
        <ChevronLeft size={16} />
        All wallets
      </button>

      {isLoading && (
        <div className={styles.centered}>
          <Spinner />
        </div>
      )}

      {!isLoading && error && <p className={styles.error}>{error}</p>}

      {!isLoading && wallet && (
        <>
          <Card className={styles.hero}>
            <div className={styles.heroTop}>
              <div>
                <p className={styles.currencyTag}>{wallet.currency}</p>
                <h1 className={styles.walletName}>{wallet.name}</h1>
              </div>
            </div>
            <p className={styles.balance}>
              <AnimatedNumber
                value={wallet.balance.amount}
                formatter={(value) => Money.of(Math.max(value, 0), wallet.currency).format()}
              />
            </p>
            <div className={styles.actions}>
              <Button variant="secondary" onClick={() => setActiveModal('deposit')}>
                <ArrowDownToLine size={16} />
                Deposit
              </Button>
              <Button variant="secondary" onClick={() => setActiveModal('withdraw')}>
                <ArrowUpFromLine size={16} />
                Withdraw
              </Button>
              <Button variant="secondary" onClick={() => setActiveModal('transfer')}>
                <ArrowLeftRight size={16} />
                Transfer
              </Button>
            </div>
          </Card>

          <section className={styles.ledgerSection}>
            <h2 className={styles.ledgerTitle}>Transaction history</h2>
            <Card>
              <TransactionList transactions={transactions} isLoading={isTransactionsLoading} />
            </Card>
          </section>

          {activeModal && (
            <MoneyMovementModal
              isOpen={Boolean(activeModal)}
              onClose={() => setActiveModal(null)}
              wallet={wallet}
              onCompleted={() => {
                void handleCompleted();
              }}
              initialTab={activeModal}
            />
          )}
        </>
      )}
    </AppShell>
  );
}

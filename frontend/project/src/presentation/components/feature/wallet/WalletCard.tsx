import { useNavigate } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import type { Wallet } from '../../../../domain/entities/Wallet';
import { Money } from '../../../../domain/value-objects/Money';
import { Card } from '../../ui/Card';
import { AnimatedNumber } from '../../ui/AnimatedNumber';
import { routes } from '../../../../shared/constants/routes';
import styles from './WalletCard.module.css';

export function WalletCard({ wallet }: { wallet: Wallet }): React.JSX.Element {
  const navigate = useNavigate();

  return (
    <Card
      interactive
      className={styles.card}
      onClick={() => {
        void navigate(routes.walletDetail(wallet.id.toString()));
      }}
      role="button"
      tabIndex={0}
      onKeyDown={(event) => {
        if (event.key === 'Enter') void navigate(routes.walletDetail(wallet.id.toString()));
      }}
    >
      <div className={styles.top}>
        <span className={styles.currencyTag}>{wallet.currency}</span>
        <ArrowUpRight size={16} className={styles.arrow} />
      </div>
      <h3 className={styles.name}>{wallet.name}</h3>
      <p className={styles.balance}>
        <AnimatedNumber
          value={wallet.balance.amount}
          formatter={(value) => Money.of(Math.max(value, 0), wallet.currency).format()}
        />
      </p>
      <p className={styles.meta}>
        {wallet.isDormant ? 'No activity yet' : `Last activity ${wallet.lastTransactionAt?.toLocaleDateString()}`}
      </p>
    </Card>
  );
}

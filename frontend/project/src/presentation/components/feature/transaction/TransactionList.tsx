import type { Transaction } from '../../../../domain/entities/Transaction';
import { TransactionRow } from './TransactionRow';
import { Spinner } from '../../ui/Spinner';
import styles from './TransactionList.module.css';

interface TransactionListProps {
  transactions: Transaction[];
  isLoading: boolean;
}

export function TransactionList({ transactions, isLoading }: TransactionListProps): React.JSX.Element {
  if (isLoading) {
    return (
      <div className={styles.centered}>
        <Spinner />
      </div>
    );
  }

  if (transactions.length === 0) {
    return (
      <div className={styles.empty}>
        <p className={styles.emptyTitle}>No transactions yet</p>
        <p className={styles.emptyBody}>Deposits, withdrawals, and transfers for this wallet will appear here.</p>
      </div>
    );
  }

  return (
    <ul className={styles.list}>
      {transactions.map((transaction) => (
        <TransactionRow key={transaction.id.toString()} transaction={transaction} />
      ))}
    </ul>
  );
}

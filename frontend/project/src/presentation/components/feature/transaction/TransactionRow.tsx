import { ArrowDownLeft, ArrowUpRight, ArrowLeftRight } from 'lucide-react';
import type { Transaction } from '../../../../domain/entities/Transaction';
import { TransactionType } from '../../../../domain/enums/TransactionType';
import { formatRelative } from '../../../../shared/utils/formatDate';
import styles from './TransactionRow.module.css';

const ICONS: Record<TransactionType, typeof ArrowDownLeft> = {
  [TransactionType.Deposit]: ArrowDownLeft,
  [TransactionType.TransferIn]: ArrowDownLeft,
  [TransactionType.Withdrawal]: ArrowUpRight,
  [TransactionType.TransferOut]: ArrowUpRight,
  [TransactionType.Unknown]: ArrowLeftRight,
};

export function TransactionRow({ transaction }: { transaction: Transaction }): React.JSX.Element {
  const Icon = ICONS[transaction.type];
  const isCredit = transaction.isCredit;

  return (
    <li className={styles.row}>
      <span className={`${styles.iconWrap} ${isCredit ? styles.credit : styles.debit}`}>
        <Icon size={15} />
      </span>
      <div className={styles.details}>
        <span className={styles.description}>
          {transaction.description || transaction.type}
        </span>
        <span className={styles.meta}>
          {formatRelative(transaction.createdAt)}
          {transaction.reference ? ` · Ref ${transaction.reference}` : ''}
          {!transaction.isCompleted ? ' · Pending' : ''}
        </span>
      </div>
      <span className={`${styles.amount} tabular-nums ${isCredit ? styles.credit : styles.debit}`}>
        {isCredit ? '+' : '-'}
        {transaction.amount.format()}
      </span>
    </li>
  );
}

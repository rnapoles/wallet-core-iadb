import type { HTMLAttributes, ReactNode } from 'react';
import clsx from 'clsx';
import styles from './Card.module.css';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  interactive?: boolean;
}

export function Card({ children, interactive = false, className, ...rest }: CardProps): React.JSX.Element {
  return (
    <div className={clsx(styles.card, interactive && styles.interactive, className)} {...rest}>
      {children}
    </div>
  );
}

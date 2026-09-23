import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import styles from './AuthLayout.module.css';

interface AuthLayoutProps {
  title: string;
  subtitle: string;
  switchPrompt: string;
  switchLabel: string;
  switchTo: string;
  children: ReactNode;
}

export function AuthLayout({
  title,
  subtitle,
  switchPrompt,
  switchLabel,
  switchTo,
  children,
}: AuthLayoutProps): React.JSX.Element {
  return (
    <div className={styles.wrapper}>
      <section className={styles.hero}>
        <div className={styles.brand}>
          <span className={styles.brandMark} aria-hidden />
          WalletSystem
        </div>
        <div className={styles.heroBody}>
          <h1 className={styles.heroHeadline}>Every wallet, every movement, one ledger.</h1>
          <p className={styles.heroSub}>
            Hold multiple currencies, move funds between wallets instantly, and see the full history
            of every deposit, withdrawal, and transfer in real time.
          </p>
        </div>
        <div className={styles.ledgerLine} aria-hidden />
      </section>

      <section className={styles.panel}>
        <div className={styles.panelInner}>
          <h2 className={styles.panelTitle}>{title}</h2>
          <p className={styles.panelSub}>
            {subtitle} {switchPrompt}{' '}
            <Link to={switchTo} className={styles.switchLink}>
              {switchLabel}
            </Link>
          </p>
          {children}
        </div>
      </section>
    </div>
  );
}

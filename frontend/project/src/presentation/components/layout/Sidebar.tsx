import { NavLink } from 'react-router-dom';
import { LayoutGrid, Wallet2, LogOut } from 'lucide-react';
import { routes } from '../../../shared/constants/routes';
import { useAuth } from '../../hooks/useAuth';
import styles from './Sidebar.module.css';

export function Sidebar(): React.JSX.Element {
  const { user, logout } = useAuth();

  return (
    <aside className={styles.sidebar}>
      <div className={styles.brand}>
        <span className={styles.brandMark} aria-hidden />
        <span className={styles.brandName}>WalletSystem</span>
      </div>

      <nav className={styles.nav} aria-label="Primary">
        <NavLink
          to={routes.dashboard}
          end
          className={({ isActive }) => `${styles.navItem} ${isActive ? styles.navItemActive : ''}`}
        >
          <LayoutGrid size={18} />
          Wallets
        </NavLink>
      </nav>

      <div className={styles.footer}>
        <div className={styles.userRow}>
          <span className={styles.avatar}>{user?.initials ?? <Wallet2 size={16} />}</span>
          <div className={styles.userMeta}>
            <span className={styles.userName}>{user?.displayName ?? 'Guest'}</span>
            <span className={styles.userEmail}>{user?.email.toString()}</span>
          </div>
        </div>
        <button type="button" className={styles.logoutButton} onClick={logout}>
          <LogOut size={16} />
          Sign out
        </button>
      </div>
    </aside>
  );
}

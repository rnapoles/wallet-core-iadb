import { Link } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { routes } from '../../shared/constants/routes';

export function NotFoundPage(): React.JSX.Element {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 'var(--space-4)',
        textAlign: 'center',
        padding: 'var(--space-6)',
      }}
    >
      <p style={{ fontFamily: 'var(--font-display)', fontSize: 'var(--text-4xl)', color: 'var(--color-signal)' }}>
        404
      </p>
      <h1 style={{ fontSize: 'var(--text-xl)' }}>This page doesn&apos;t exist</h1>
      <p style={{ color: 'var(--color-text-muted)', maxWidth: 380 }}>
        The page you&apos;re looking for may have moved or never existed.
      </p>
      <Link to={routes.dashboard}>
        <Button variant="secondary">Back to wallets</Button>
      </Link>
    </div>
  );
}

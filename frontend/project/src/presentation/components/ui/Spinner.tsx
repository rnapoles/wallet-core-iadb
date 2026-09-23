import { Loader2 } from 'lucide-react';

export function Spinner({ size = 22, label = 'Loading' }: { size?: number; label?: string }): React.JSX.Element {
  return (
    <div role="status" aria-label={label} style={{ display: 'inline-flex' }}>
      <Loader2
        size={size}
        color="var(--color-signal)"
        style={{ animation: 'spin 0.8s linear infinite' }}
      />
      <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
}

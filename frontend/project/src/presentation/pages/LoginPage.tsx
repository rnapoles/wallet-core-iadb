import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthLayout } from '../components/layout/AuthLayout';
import { LoginForm } from '../components/feature/auth/LoginForm';
import { SelectField } from '../components/ui/SelectField';
import { useAuth } from '../hooks/useAuth';
import { useToast } from '../hooks/useToast';
import { bankInfoUsers, findBankInfoUserById } from '../../shared/data/bankInfo';
import { getErrorMessage } from '../../shared/utils/getErrorMessage';
import { routes } from '../../shared/constants/routes';

export function LoginPage(): React.JSX.Element {
  const { login } = useAuth();
  const { notify } = useToast();
  const navigate = useNavigate();

  const [selectedUserId, setSelectedUserId] = useState('');
  const [isQuickSigningIn, setIsQuickSigningIn] = useState(false);

  async function handleQuickLogin(userId: string): Promise<void> {
    const demoUser = findBankInfoUserById(userId);
    if (!demoUser) return;

    setIsQuickSigningIn(true);
    try {
      await login(demoUser.email, demoUser.password);
      void navigate(routes.dashboard);
    } catch (error) {
      notify(getErrorMessage(error), 'error');
      setSelectedUserId('');
    } finally {
      setIsQuickSigningIn(false);
    }
  }

  return (
    <AuthLayout
      title="Welcome back"
      subtitle="Sign in to access your wallets."
      switchPrompt="New here?"
      switchLabel="Create an account"
      switchTo={routes.register}
    >
      <SelectField
        label="Quick demo sign-in"
        value={selectedUserId}
        disabled={isQuickSigningIn}
        onChange={(event) => {
          const userId = event.target.value;
          setSelectedUserId(userId);
          if (userId) void handleQuickLogin(userId);
        }}
        hint="Seeded demo accounts — selecting one signs you in automatically."
        style={{ marginBottom: 'var(--space-5)' }}
      >
        <option value="">Select a demo user…</option>
        {bankInfoUsers.map((demoUser) => (
          <option key={demoUser.id} value={demoUser.id}>
            {demoUser.firstName} {demoUser.lastName} — {demoUser.email}
          </option>
        ))}
      </SelectField>

      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 'var(--space-3)',
          margin: '0 0 var(--space-5)',
          color: 'var(--color-text-faint)',
          fontSize: 'var(--text-xs)',
        }}
      >
        <span style={{ flex: 1, height: 1, background: 'var(--color-border)' }} />
        or sign in manually
        <span style={{ flex: 1, height: 1, background: 'var(--color-border)' }} />
      </div>

      <LoginForm />
    </AuthLayout>
  );
}

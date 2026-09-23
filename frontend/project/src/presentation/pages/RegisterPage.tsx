import { AuthLayout } from '../components/layout/AuthLayout';
import { RegisterForm } from '../components/feature/auth/RegisterForm';
import { routes } from '../../shared/constants/routes';

export function RegisterPage(): React.JSX.Element {
  return (
    <AuthLayout
      title="Create your account"
      subtitle="Open wallets in seconds."
      switchPrompt="Already registered?"
      switchLabel="Sign in"
      switchTo={routes.login}
    >
      <RegisterForm />
    </AuthLayout>
  );
}

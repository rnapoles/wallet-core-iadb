import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { TextField } from '../../ui/TextField';
import { Button } from '../../ui/Button';
import { useAuth } from '../../../hooks/useAuth';
import { useToast } from '../../../hooks/useToast';
import { getErrorMessage } from '../../../../shared/utils/getErrorMessage';
import { ValidationError } from '../../../../domain/errors/ValidationError';
import { routes } from '../../../../shared/constants/routes';

export function RegisterForm(): React.JSX.Element {
  const { register } = useAuth();
  const { notify } = useToast();
  const navigate = useNavigate();

  const [values, setValues] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  function update(field: keyof typeof values) {
    return (event: React.ChangeEvent<HTMLInputElement>) =>
      setValues((current) => ({ ...current, [field]: event.target.value }));
  }

  async function handleSubmit(event: FormEvent): Promise<void> {
    event.preventDefault();
    setFieldErrors({});
    setIsSubmitting(true);
    try {
      await register(values);
      notify('Account created. Welcome aboard.', 'success');
      void navigate(routes.dashboard);
    } catch (error) {
      if (error instanceof ValidationError) {
        setFieldErrors(error.fieldErrors);
      } else {
        notify(getErrorMessage(error), 'error');
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form
      onSubmit={(event) => {
        void handleSubmit(event);
      }}
      noValidate
      style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
    >
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 'var(--space-4)' }}>
        <TextField
          label="First name"
          name="firstName"
          autoComplete="given-name"
          value={values.firstName}
          onChange={update('firstName')}
          error={fieldErrors.firstName}
          required
        />
        <TextField
          label="Last name"
          name="lastName"
          autoComplete="family-name"
          value={values.lastName}
          onChange={update('lastName')}
          error={fieldErrors.lastName}
          required
        />
      </div>
      <TextField
        label="Email"
        type="email"
        name="email"
        autoComplete="email"
        value={values.email}
        onChange={update('email')}
        error={fieldErrors.email}
        required
      />
      <TextField
        label="Password"
        type="password"
        name="password"
        autoComplete="new-password"
        value={values.password}
        onChange={update('password')}
        error={fieldErrors.password}
        hint="At least 8 characters, one uppercase letter, one number."
        required
      />
      <TextField
        label="Confirm password"
        type="password"
        name="confirmPassword"
        autoComplete="new-password"
        value={values.confirmPassword}
        onChange={update('confirmPassword')}
        error={fieldErrors.confirmPassword}
        required
      />
      <Button type="submit" isLoading={isSubmitting} style={{ marginTop: 'var(--space-2)' }}>
        Create account
      </Button>
    </form>
  );
}

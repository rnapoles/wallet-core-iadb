import { useState, type FormEvent } from 'react';
import { Modal } from '../../ui/Modal';
import { TextField } from '../../ui/TextField';
import { SelectField } from '../../ui/SelectField';
import { Button } from '../../ui/Button';
import { SUPPORTED_CURRENCIES } from '../../../../domain/enums/Currency';
import type { Wallet } from '../../../../domain/entities/Wallet';
import { container } from '../../../../infrastructure/config/container';
import { useAuth } from '../../../hooks/useAuth';
import { useToast } from '../../../hooks/useToast';
import { getErrorMessage } from '../../../../shared/utils/getErrorMessage';
import { ValidationError } from '../../../../domain/errors/ValidationError';

interface CreateWalletModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: (wallet: Wallet) => void;
}

export function CreateWalletModal({ isOpen, onClose, onCreated }: CreateWalletModalProps): React.JSX.Element {
  const { user } = useAuth();
  const { notify } = useToast();
  const [name, setName] = useState('');
  const [currency, setCurrency] = useState(SUPPORTED_CURRENCIES[0] ?? 'USD');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent): Promise<void> {
    event.preventDefault();
    if (!user) return;
    setFieldErrors({});
    setIsSubmitting(true);
    try {
      const wallet = await container.walletService.createWallet(user.id.toString(), { name, currency });
      notify(`"${wallet.name}" is ready to use.`, 'success');
      onCreated(wallet);
      setName('');
      onClose();
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
    <Modal isOpen={isOpen} onClose={onClose} title="Open a new wallet">
      <form
        onSubmit={(event) => {
          void handleSubmit(event);
        }}
        noValidate
        style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
      >
        <TextField
          label="Wallet name"
          placeholder="e.g. Travel fund"
          value={name}
          onChange={(event) => setName(event.target.value)}
          error={fieldErrors.name}
          autoFocus
          required
        />
        <SelectField
          label="Currency"
          value={currency}
          onChange={(event) => setCurrency(event.target.value)}
          error={fieldErrors.currency}
        >
          {SUPPORTED_CURRENCIES.map((code) => (
            <option key={code} value={code}>
              {code}
            </option>
          ))}
        </SelectField>
        <Button type="submit" isLoading={isSubmitting} style={{ marginTop: 'var(--space-2)' }}>
          Create wallet
        </Button>
      </form>
    </Modal>
  );
}

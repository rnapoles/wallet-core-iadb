import { useMemo, useState, type FormEvent } from 'react';
import type { Wallet } from '../../../../domain/entities/Wallet';
import { Modal } from '../../ui/Modal';
import { TextField } from '../../ui/TextField';
import { SelectField } from '../../ui/SelectField';
import { Button } from '../../ui/Button';
import { container } from '../../../../infrastructure/config/container';
import { useToast } from '../../../hooks/useToast';
import { getErrorMessage } from '../../../../shared/utils/getErrorMessage';
import { ValidationError } from '../../../../domain/errors/ValidationError';
import { getTransferCandidates, getWalletOwnerName } from '../../../../shared/data/bankInfo';
import styles from './MoneyMovementModal.module.css';

type MovementKind = 'deposit' | 'withdraw' | 'transfer';

interface MoneyMovementModalProps {
  isOpen: boolean;
  onClose: () => void;
  wallet: Wallet;
  onCompleted: () => void;
  initialTab?: MovementKind;
}

const TAB_LABELS: Record<MovementKind, string> = {
  deposit: 'Deposit',
  withdraw: 'Withdraw',
  transfer: 'Transfer',
};

export function MoneyMovementModal({
  isOpen,
  onClose,
  wallet,
  onCompleted,
  initialTab = 'deposit',
}: MoneyMovementModalProps): React.JSX.Element {
  const { notify } = useToast();
  const [tab, setTab] = useState<MovementKind>(initialTab);
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [reference, setReference] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Destination wallets, sourced from the seeded bank-info dataset rather than
  // GET /api/wallets: the live endpoint only returns the signed-in user's own
  // wallets, but a transfer can target any wallet — as long as its currency
  // matches the source wallet's, since the API has no FX conversion.
  const transferCandidates = useMemo(
    () => getTransferCandidates(wallet.id.toString(), wallet.currency),
    [wallet.id, wallet.currency],
  );
  const [toWalletId, setToWalletId] = useState(transferCandidates[0]?.id ?? '');

  function resetAndClose(): void {
    setAmount('');
    setDescription('');
    setReference('');
    setFieldErrors({});
    onClose();
  }

  async function handleSubmit(event: FormEvent): Promise<void> {
    event.preventDefault();
    setFieldErrors({});
    setIsSubmitting(true);
    const numericAmount = Number(amount);
    try {
      if (tab === 'deposit') {
        const result = await container.transactionService.deposit({
          walletId: wallet.id.toString(),
          amount: numericAmount,
          description,
          reference,
        });
        notify(result.message ?? 'Deposit completed.', 'success');
      } else if (tab === 'withdraw') {
        const result = await container.transactionService.withdraw(wallet, {
          walletId: wallet.id.toString(),
          amount: numericAmount,
          description,
          reference,
        });
        notify(result.message ?? 'Withdrawal completed.', 'success');
      } else {
        const result = await container.transactionService.transfer(wallet, {
          fromWalletId: wallet.id.toString(),
          toWalletId,
          amount: numericAmount,
          description,
          reference,
        });
        notify(result.message ?? 'Transfer completed.', 'success');
      }
      onCompleted();
      resetAndClose();
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
    <Modal isOpen={isOpen} onClose={resetAndClose} title={`${TAB_LABELS[tab]} — ${wallet.name}`}>
      <div className={styles.tabs} role="tablist">
        {(Object.keys(TAB_LABELS) as MovementKind[]).map((kind) => (
          <button
            key={kind}
            type="button"
            role="tab"
            aria-selected={tab === kind}
            className={`${styles.tab} ${tab === kind ? styles.tabActive : ''}`}
            onClick={() => setTab(kind)}
          >
            {TAB_LABELS[kind]}
          </button>
        ))}
      </div>

      <form
        onSubmit={(event) => {
          void handleSubmit(event);
        }}
        noValidate
        style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}
      >
        {tab === 'transfer' && (
          <SelectField
            label="Destination wallet"
            value={toWalletId}
            onChange={(event) => setToWalletId(event.target.value)}
            error={fieldErrors.toWalletId}
            hint={`Showing ${wallet.currency} wallets only — transfers don't convert currency.`}
          >
            {transferCandidates.length === 0 && (
              <option value="">No {wallet.currency} wallets available to transfer to</option>
            )}
            {transferCandidates.map((candidate) => (
              <option key={candidate.id} value={candidate.id}>
                {candidate.name} — {getWalletOwnerName(candidate.userId)} ({candidate.currency})
              </option>
            ))}
          </SelectField>
        )}

        <TextField
          label={`Amount (${wallet.currency})`}
          type="number"
          inputMode="decimal"
          step="0.01"
          min="0"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          error={fieldErrors.amount}
          hint={tab !== 'deposit' ? `Available: ${wallet.balance.format()}` : undefined}
          required
        />
        <TextField
          label="Description (optional)"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          error={fieldErrors.description}
          maxLength={200}
        />
        <TextField
          label="Reference (optional)"
          value={reference}
          onChange={(event) => setReference(event.target.value)}
          error={fieldErrors.reference}
          maxLength={100}
        />

        <Button
          type="submit"
          isLoading={isSubmitting}
          disabled={tab === 'transfer' && transferCandidates.length === 0}
          style={{ marginTop: 'var(--space-2)' }}
        >
          Confirm {TAB_LABELS[tab].toLowerCase()}
        </Button>
      </form>
    </Modal>
  );
}

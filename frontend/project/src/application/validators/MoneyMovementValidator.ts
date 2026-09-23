import { z } from 'zod';

/**
 * Shared validation rules for deposit/withdraw amounts.
 * `maxAmount`, when provided, enforces "cannot exceed current balance" for withdrawals.
 */
export function buildAmountSchema(maxAmount?: number) {
  let schema = z
    .number({ invalid_type_error: 'Enter a numeric amount.' })
    .positive('Amount must be greater than zero.')
    .finite('Enter a valid amount.');
  if (typeof maxAmount === 'number') {
    schema = schema.max(maxAmount, 'Amount exceeds the available balance.');
  }
  return schema;
}

export const depositSchema = z.object({
  walletId: z.string().uuid('Select a wallet.'),
  amount: buildAmountSchema(),
  description: z.string().trim().max(200).optional().or(z.literal('')),
  reference: z.string().trim().max(100).optional().or(z.literal('')),
});

export type DepositFormValues = z.infer<typeof depositSchema>;

export function buildWithdrawSchema(availableBalance: number) {
  return z.object({
    walletId: z.string().uuid('Select a wallet.'),
    amount: buildAmountSchema(availableBalance),
    description: z.string().trim().max(200).optional().or(z.literal('')),
    reference: z.string().trim().max(100).optional().or(z.literal('')),
  });
}

export type WithdrawFormValues = z.infer<ReturnType<typeof buildWithdrawSchema>>;

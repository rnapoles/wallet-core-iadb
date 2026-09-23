import { z } from 'zod';
import { buildAmountSchema } from './MoneyMovementValidator';

/** Validation rules for wallet-to-wallet transfers (TransferCommand). */
export function buildTransferSchema(availableBalance: number) {
  return z
    .object({
      fromWalletId: z.string().uuid('Select a source wallet.'),
      toWalletId: z.string().uuid('Select a destination wallet.'),
      amount: buildAmountSchema(availableBalance),
      description: z.string().trim().max(200).optional().or(z.literal('')),
      reference: z.string().trim().max(100).optional().or(z.literal('')),
    })
    .refine((data) => data.fromWalletId !== data.toWalletId, {
      message: 'Source and destination wallets must be different.',
      path: ['toWalletId'],
    });
}

export type TransferFormValues = z.infer<ReturnType<typeof buildTransferSchema>>;

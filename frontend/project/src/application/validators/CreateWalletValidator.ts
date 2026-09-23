import { z } from 'zod';
import { SUPPORTED_CURRENCIES } from '../../domain/enums/Currency';

/** Validation rules for wallet creation (CreateWalletCommand). */
export const createWalletSchema = z.object({
  name: z.string().trim().min(2, 'Name must be at least 2 characters.').max(60),
  currency: z.enum(SUPPORTED_CURRENCIES as [string, ...string[]], {
    errorMap: () => ({ message: 'Choose a supported currency.' }),
  }),
});

export type CreateWalletFormValues = z.infer<typeof createWalletSchema>;

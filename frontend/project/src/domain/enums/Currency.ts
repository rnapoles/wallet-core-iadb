/**
 * Supported wallet currencies.
 * Kept as a closed set so the UI can render correct symbols and formatting
 * without trusting free-text input from the API.
 */
export enum Currency {
  USD = 'USD',
  EUR = 'EUR',
  GBP = 'GBP',
  MXN = 'MXN',
  JPY = 'JPY',
  CAD = 'CAD',
  BTC = 'BTC',
  ETH = 'ETH',
}

export const SUPPORTED_CURRENCIES: readonly Currency[] = Object.values(Currency);

export function isCurrency(value: string): value is Currency {
  return (SUPPORTED_CURRENCIES as string[]).includes(value);
}

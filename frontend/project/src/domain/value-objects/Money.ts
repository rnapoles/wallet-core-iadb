import { Currency } from '../enums/Currency';

/**
 * Immutable value object representing a monetary amount in a given currency.
 * Centralizes the invariant "amounts are non-negative finite numbers" so it
 * cannot be bypassed by constructing a raw number somewhere in the UI.
 */
export class Money {
  private constructor(
    private readonly amountMinorAgnostic: number,
    private readonly currencyCode: Currency,
  ) {}

  public static of(amount: number, currency: Currency): Money {
    if (!Number.isFinite(amount)) {
      throw new RangeError('Money amount must be a finite number.');
    }
    if (amount < 0) {
      throw new RangeError('Money amount cannot be negative.');
    }
    return new Money(amount, currency);
  }

  public static zero(currency: Currency): Money {
    return new Money(0, currency);
  }

  public get amount(): number {
    return this.amountMinorAgnostic;
  }

  public get currency(): Currency {
    return this.currencyCode;
  }

  public add(other: Money): Money {
    this.assertSameCurrency(other);
    return Money.of(this.amount + other.amount, this.currencyCode);
  }

  public subtract(other: Money): Money {
    this.assertSameCurrency(other);
    return Money.of(this.amount - other.amount, this.currencyCode);
  }

  public isGreaterThanOrEqual(other: Money): boolean {
    this.assertSameCurrency(other);
    return this.amount >= other.amount;
  }

  public format(locale = 'en-US'): string {
    if (this.currencyCode === Currency.BTC || this.currencyCode === Currency.ETH) {
      return `${this.amount.toFixed(6)} ${this.currencyCode}`;
    }
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: this.currencyCode,
      currencyDisplay: 'narrowSymbol',
    }).format(this.amount);
  }

  private assertSameCurrency(other: Money): void {
    if (other.currency !== this.currencyCode) {
      throw new TypeError(
        `Currency mismatch: cannot combine ${this.currencyCode} with ${other.currency}.`,
      );
    }
  }
}

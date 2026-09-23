import type { Currency } from '../enums/Currency';
import type { EntityId } from '../value-objects/EntityId';
import type { Money } from '../value-objects/Money';

/**
 * Core Wallet entity: a named balance held by a user in a single currency.
 */
export class Wallet {
  public constructor(
    public readonly id: EntityId,
    public readonly name: string,
    public readonly balance: Money,
    public readonly createdAt: Date,
    public readonly lastTransactionAt: Date | null,
  ) {}

  public get currency(): Currency {
    return this.balance.currency;
  }

  public canAfford(amount: Money): boolean {
    return this.balance.isGreaterThanOrEqual(amount);
  }

  public get isDormant(): boolean {
    return this.lastTransactionAt === null;
  }
}

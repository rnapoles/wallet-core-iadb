import { TransactionType } from '../enums/TransactionType';
import type { EntityId } from '../value-objects/EntityId';
import type { Money } from '../value-objects/Money';

/**
 * Core Transaction entity: an immutable ledger record for a single wallet movement.
 */
export class Transaction {
  public constructor(
    public readonly id: EntityId,
    public readonly type: TransactionType,
    public readonly amount: Money,
    public readonly walletId: EntityId,
    public readonly relatedWalletId: EntityId | null,
    public readonly description: string | null,
    public readonly reference: string | null,
    public readonly createdAt: Date,
    public readonly isCompleted: boolean,
  ) {}

  public get isCredit(): boolean {
    return this.type === TransactionType.Deposit || this.type === TransactionType.TransferIn;
  }

  public get isDebit(): boolean {
    return this.type === TransactionType.Withdrawal || this.type === TransactionType.TransferOut;
  }

  public get signedAmount(): number {
    return this.isDebit ? -this.amount.amount : this.amount.amount;
  }
}

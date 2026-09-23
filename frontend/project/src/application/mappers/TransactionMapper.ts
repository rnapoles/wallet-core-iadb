import type { TransactionDTO } from '../dto/transaction/TransactionDTO';
import { Transaction } from '../../domain/entities/Transaction';
import type { Currency } from '../../domain/enums/Currency';
import { toTransactionType } from '../../domain/enums/TransactionType';
import { EntityId } from '../../domain/value-objects/EntityId';
import { Money } from '../../domain/value-objects/Money';

/**
 * Maps transaction DTOs to the domain Transaction entity.
 * The ledger endpoints don't echo a currency, so `walletCurrency` must be
 * supplied by the caller (looked up from the owning wallet).
 */
export class TransactionMapper {
  public static fromDTO(dto: TransactionDTO, walletCurrency: Currency): Transaction {
    return new Transaction(
      EntityId.create(dto.id),
      toTransactionType(dto.type),
      Money.of(dto.amount, walletCurrency),
      EntityId.create(dto.walletId),
      dto.relatedWalletId ? EntityId.create(dto.relatedWalletId) : null,
      dto.description,
      dto.reference,
      new Date(dto.createdAt),
      dto.isCompleted,
    );
  }

  public static fromDTOList(
    dtos: readonly TransactionDTO[],
    walletCurrency: Currency,
  ): Transaction[] {
    return dtos.map((dto) => TransactionMapper.fromDTO(dto, walletCurrency));
  }
}

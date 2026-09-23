import type { WalletDTO } from '../dto/wallet/WalletDTO';
import { Wallet } from '../../domain/entities/Wallet';
import { Currency, isCurrency } from '../../domain/enums/Currency';
import { EntityId } from '../../domain/value-objects/EntityId';
import { Money } from '../../domain/value-objects/Money';

/** Maps wallet DTOs to the domain Wallet entity. */
export class WalletMapper {
  public static fromDTO(dto: WalletDTO): Wallet {
    const currencyCode = dto.currency ?? '';
    const currency = isCurrency(currencyCode) ? currencyCode : Currency.USD;
    return new Wallet(
      EntityId.create(dto.id),
      dto.name ?? 'Untitled wallet',
      Money.of(dto.balance, currency),
      new Date(dto.createdAt),
      dto.lastTransactionAt ? new Date(dto.lastTransactionAt) : null,
    );
  }

  public static fromDTOList(dtos: readonly WalletDTO[]): Wallet[] {
    return dtos.map((dto) => WalletMapper.fromDTO(dto));
  }
}

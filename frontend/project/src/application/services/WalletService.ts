import type { IWalletRepository } from '../../domain/repositories/IWalletRepository';
import type { Wallet } from '../../domain/entities/Wallet';
import { ValidationError } from '../../domain/errors/ValidationError';
import { WalletMapper } from '../mappers/WalletMapper';
import { sanitizeUserInput } from '../../infrastructure/security/sanitize';
import { createWalletSchema, type CreateWalletFormValues } from '../validators/CreateWalletValidator';
import { zodIssuesToFieldErrors } from '../validators/zodIssuesToFieldErrors';

/** Application service for wallet use cases. */
export class WalletService {
  public constructor(private readonly walletRepository: IWalletRepository) {}

  public async listWallets(): Promise<Wallet[]> {
    const dtos = await this.walletRepository.list();
    return WalletMapper.fromDTOList(dtos);
  }

  public async getWallet(id: string): Promise<Wallet> {
    const dto = await this.walletRepository.getById(id);
    return WalletMapper.fromDTO(dto);
  }

  public async createWallet(userId: string, values: CreateWalletFormValues): Promise<Wallet> {
    const parsed = createWalletSchema.safeParse(values);
    if (!parsed.success) {
      throw new ValidationError(zodIssuesToFieldErrors(parsed.error));
    }
    const response = await this.walletRepository.create({
      userId,
      name: sanitizeUserInput(parsed.data.name, 60),
      currency: parsed.data.currency,
    });
    return WalletMapper.fromDTO({
      id: response.walletId,
      name: response.name,
      currency: response.currency,
      balance: response.balance,
      createdAt: new Date().toISOString(),
      lastTransactionAt: null,
    });
  }
}

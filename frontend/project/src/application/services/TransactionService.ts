import type { ITransactionRepository } from '../../domain/repositories/ITransactionRepository';
import type { Wallet } from '../../domain/entities/Wallet';
import type { Transaction } from '../../domain/entities/Transaction';
import { ValidationError } from '../../domain/errors/ValidationError';
import { InsufficientFundsError } from '../../domain/errors/InsufficientFundsError';
import { TransactionMapper } from '../mappers/TransactionMapper';
import { sanitizeUserInput } from '../../infrastructure/security/sanitize';
import { depositSchema, buildWithdrawSchema, type DepositFormValues, type WithdrawFormValues } from '../validators/MoneyMovementValidator';
import { buildTransferSchema, type TransferFormValues } from '../validators/TransferValidator';
import { zodIssuesToFieldErrors } from '../validators/zodIssuesToFieldErrors';

/** Application service for transaction (ledger) use cases. */
export class TransactionService {
  public constructor(private readonly transactionRepository: ITransactionRepository) {}

  public async deposit(values: DepositFormValues): Promise<{ newBalance: number; message: string | null }> {
    const parsed = depositSchema.safeParse(values);
    if (!parsed.success) {
      throw new ValidationError(zodIssuesToFieldErrors(parsed.error));
    }
    const response = await this.transactionRepository.deposit({
      walletId: parsed.data.walletId,
      amount: parsed.data.amount,
      description: parsed.data.description ? sanitizeUserInput(parsed.data.description, 200) : null,
      reference: parsed.data.reference ? sanitizeUserInput(parsed.data.reference, 100) : null,
    });
    return { newBalance: response.newBalance, message: response.message };
  }

  public async withdraw(
    sourceWallet: Wallet,
    values: WithdrawFormValues,
  ): Promise<{ newBalance: number; message: string | null }> {
    if (values.amount > sourceWallet.balance.amount) {
      throw new InsufficientFundsError(sourceWallet.name);
    }
    const parsed = buildWithdrawSchema(sourceWallet.balance.amount).safeParse(values);
    if (!parsed.success) {
      throw new ValidationError(zodIssuesToFieldErrors(parsed.error));
    }
    const response = await this.transactionRepository.withdraw({
      walletId: parsed.data.walletId,
      amount: parsed.data.amount,
      description: parsed.data.description ? sanitizeUserInput(parsed.data.description, 200) : null,
      reference: parsed.data.reference ? sanitizeUserInput(parsed.data.reference, 100) : null,
    });
    return { newBalance: response.newBalance, message: response.message };
  }

  public async transfer(
    sourceWallet: Wallet,
    values: TransferFormValues,
  ): Promise<{ newBalance: number; message: string | null }> {
    if (values.amount > sourceWallet.balance.amount) {
      throw new InsufficientFundsError(sourceWallet.name);
    }
    const parsed = buildTransferSchema(sourceWallet.balance.amount).safeParse(values);
    if (!parsed.success) {
      throw new ValidationError(zodIssuesToFieldErrors(parsed.error));
    }
    const response = await this.transactionRepository.transfer({
      fromWalletId: parsed.data.fromWalletId,
      toWalletId: parsed.data.toWalletId,
      amount: parsed.data.amount,
      description: parsed.data.description ? sanitizeUserInput(parsed.data.description, 200) : null,
      reference: parsed.data.reference ? sanitizeUserInput(parsed.data.reference, 100) : null,
    });
    return { newBalance: response.newBalance, message: response.message };
  }

  public async getTransaction(id: string, walletCurrency: Wallet['currency']): Promise<Transaction> {
    const dto = await this.transactionRepository.getById(id);
    return TransactionMapper.fromDTO(dto, walletCurrency);
  }

  public async listByWallet(wallet: Wallet): Promise<Transaction[]> {
    const dtos = await this.transactionRepository.listByWallet(wallet.id.toString());
    return TransactionMapper.fromDTOList(dtos, wallet.currency).sort(
      (a, b) => b.createdAt.getTime() - a.createdAt.getTime(),
    );
  }
}

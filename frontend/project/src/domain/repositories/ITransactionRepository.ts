import type { DepositRequestDTO } from '../../application/dto/transaction/DepositRequestDTO';
import type { TransactionDTO } from '../../application/dto/transaction/TransactionDTO';
import type { TransactionResponseDTO } from '../../application/dto/transaction/TransactionResponseDTO';
import type { TransferRequestDTO } from '../../application/dto/transaction/TransferRequestDTO';
import type { WithdrawRequestDTO } from '../../application/dto/transaction/WithdrawRequestDTO';

/**
 * Port describing transaction operations, mapped to `/api/transactions/*`.
 */
export interface ITransactionRepository {
  deposit(request: DepositRequestDTO): Promise<TransactionResponseDTO>;
  withdraw(request: WithdrawRequestDTO): Promise<TransactionResponseDTO>;
  transfer(request: TransferRequestDTO): Promise<TransactionResponseDTO>;
  getById(id: string): Promise<TransactionDTO>;
  listByWallet(walletId: string): Promise<TransactionDTO[]>;
}

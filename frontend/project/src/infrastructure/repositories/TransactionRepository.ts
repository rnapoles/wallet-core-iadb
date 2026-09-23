import type { ITransactionRepository } from '../../domain/repositories/ITransactionRepository';
import type { DepositRequestDTO } from '../../application/dto/transaction/DepositRequestDTO';
import type { TransactionDTO } from '../../application/dto/transaction/TransactionDTO';
import type { TransactionResponseDTO } from '../../application/dto/transaction/TransactionResponseDTO';
import type { TransferRequestDTO } from '../../application/dto/transaction/TransferRequestDTO';
import type { WithdrawRequestDTO } from '../../application/dto/transaction/WithdrawRequestDTO';
import type { HttpClient } from '../http/HttpClient';
import { apiEndpoints } from '../http/apiEndpoints';

/** Concrete `ITransactionRepository` backed by the WalletSystem REST API. */
export class TransactionRepository implements ITransactionRepository {
  public constructor(private readonly http: HttpClient) {}

  public deposit(request: DepositRequestDTO): Promise<TransactionResponseDTO> {
    return this.http.post<TransactionResponseDTO>(apiEndpoints.transactions.deposit, request);
  }

  public withdraw(request: WithdrawRequestDTO): Promise<TransactionResponseDTO> {
    return this.http.post<TransactionResponseDTO>(apiEndpoints.transactions.withdraw, request);
  }

  public transfer(request: TransferRequestDTO): Promise<TransactionResponseDTO> {
    return this.http.post<TransactionResponseDTO>(apiEndpoints.transactions.transfer, request);
  }

  public getById(id: string): Promise<TransactionDTO> {
    return this.http.get<TransactionDTO>(apiEndpoints.transactions.byId(id));
  }

  public listByWallet(walletId: string): Promise<TransactionDTO[]> {
    return this.http.get<TransactionDTO[]>(apiEndpoints.transactions.byWallet(walletId));
  }
}

import type { IWalletRepository } from '../../domain/repositories/IWalletRepository';
import type { CreateWalletRequestDTO } from '../../application/dto/wallet/CreateWalletRequestDTO';
import type { CreateWalletResponseDTO } from '../../application/dto/wallet/CreateWalletResponseDTO';
import type { WalletDTO } from '../../application/dto/wallet/WalletDTO';
import type { HttpClient } from '../http/HttpClient';
import { apiEndpoints } from '../http/apiEndpoints';

/** Concrete `IWalletRepository` backed by the WalletSystem REST API. */
export class WalletRepository implements IWalletRepository {
  public constructor(private readonly http: HttpClient) {}

  public list(): Promise<WalletDTO[]> {
    return this.http.get<WalletDTO[]>(apiEndpoints.wallets.collection);
  }

  public getById(id: string): Promise<WalletDTO> {
    return this.http.get<WalletDTO>(apiEndpoints.wallets.byId(id));
  }

  public create(request: CreateWalletRequestDTO): Promise<CreateWalletResponseDTO> {
    return this.http.post<CreateWalletResponseDTO>(apiEndpoints.wallets.collection, request);
  }
}

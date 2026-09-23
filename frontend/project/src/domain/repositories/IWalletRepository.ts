import type { CreateWalletRequestDTO } from '../../application/dto/wallet/CreateWalletRequestDTO';
import type { CreateWalletResponseDTO } from '../../application/dto/wallet/CreateWalletResponseDTO';
import type { WalletDTO } from '../../application/dto/wallet/WalletDTO';

/**
 * Port describing wallet operations, mapped 1:1 to the `/api/wallets` resource.
 */
export interface IWalletRepository {
  list(): Promise<WalletDTO[]>;
  getById(id: string): Promise<WalletDTO>;
  create(request: CreateWalletRequestDTO): Promise<CreateWalletResponseDTO>;
}

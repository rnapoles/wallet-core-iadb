import type { CurrentUserResponseDTO } from '../dto/auth/CurrentUserResponseDTO';
import { User } from '../../domain/entities/User';
import { Email } from '../../domain/value-objects/Email';
import { EntityId } from '../../domain/value-objects/EntityId';

/** Maps auth-related DTOs to the domain User entity. */
export class UserMapper {
  public static fromCurrentUserDTO(dto: CurrentUserResponseDTO): User {
    return new User(
      EntityId.create(dto.userId),
      Email.create(dto.email ?? ''),
      dto.firstName,
      dto.lastName,
    );
  }
}

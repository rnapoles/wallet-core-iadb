import type { Email } from '../value-objects/Email';
import type { EntityId } from '../value-objects/EntityId';

/**
 * Core User entity. Represents the authenticated account holder.
 * Free of any HTTP/DTO concerns — those live in the application layer.
 */
export class User {
  public constructor(
    public readonly id: EntityId,
    public readonly email: Email,
    public readonly firstName: string | null,
    public readonly lastName: string | null,
  ) {}

  public get displayName(): string {
    const full = [this.firstName, this.lastName].filter(Boolean).join(' ').trim();
    return full.length > 0 ? full : this.email.toString();
  }

  public get initials(): string {
    const first = this.firstName?.[0] ?? this.email.toString()[0] ?? '?';
    const last = this.lastName?.[0] ?? '';
    return `${first}${last}`.toUpperCase();
  }
}

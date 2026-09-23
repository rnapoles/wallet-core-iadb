const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Immutable value object wrapping a UUID identifier (wallet, user, transaction, ...).
 * Prevents accidentally passing an arbitrary string where an identifier is expected.
 */
export class EntityId {
  private constructor(private readonly value: string) {}

  public static create(raw: string): EntityId {
    if (!UUID_PATTERN.test(raw)) {
      throw new TypeError(`"${raw}" is not a valid UUID.`);
    }
    return new EntityId(raw);
  }

  public equals(other: EntityId): boolean {
    return this.value.toLowerCase() === other.value.toLowerCase();
  }

  public toString(): string {
    return this.value;
  }
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Immutable value object guaranteeing a syntactically valid, normalized email.
 */
export class Email {
  private constructor(private readonly value: string) {}

  public static create(raw: string): Email {
    const trimmed = raw.trim();
    if (!EMAIL_PATTERN.test(trimmed)) {
      throw new TypeError('Invalid email address format.');
    }
    return new Email(trimmed.toLowerCase());
  }

  public toString(): string {
    return this.value;
  }
}

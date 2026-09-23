/**
 * Base class for all domain-level errors. Distinguishing these from generic
 * `Error`s lets the presentation layer decide how to render them without
 * string-matching messages.
 */
export abstract class DomainError extends Error {
  protected constructor(message: string) {
    super(message);
    this.name = new.target.name;
    Object.setPrototypeOf(this, new.target.prototype);
  }
}

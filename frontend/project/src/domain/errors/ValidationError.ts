import { DomainError } from './DomainError';

/**
 * Raised when input fails validator rules before ever reaching the network.
 * `fieldErrors` maps a field name to a human-readable message for form UIs.
 */
export class ValidationError extends DomainError {
  public constructor(public readonly fieldErrors: Readonly<Record<string, string>>) {
    super('One or more fields are invalid.');
  }
}

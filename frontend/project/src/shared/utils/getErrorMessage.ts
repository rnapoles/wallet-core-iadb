import { ApiError } from '../../domain/errors/ApiError';
import { ValidationError } from '../../domain/errors/ValidationError';
import { DomainError } from '../../domain/errors/DomainError';

/** Extracts a user-safe message from any thrown value, without leaking internals. */
export function getErrorMessage(error: unknown): string {
  if (error instanceof ValidationError) {
    return Object.values(error.fieldErrors)[0] ?? 'Please check the form and try again.';
  }
  if (error instanceof ApiError) {
    if (error.isUnauthorized) return 'Your session has expired. Please sign in again.';
    if (error.isServerError) return 'Something went wrong on our end. Please try again shortly.';
    return error.message;
  }
  if (error instanceof DomainError) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred.';
}

/**
 * Defense-in-depth text sanitization. React already escapes interpolated
 * text in JSX, so this is not what prevents XSS — it exists for the few
 * places where user-supplied strings (transaction descriptions/references)
 * are sent back out to the API or logged, so control characters and
 * stray markup can't be smuggled through the client.
 */

// eslint-disable-next-line no-control-regex -- intentional: stripping ASCII control characters is the point of this regex.
const CONTROL_CHARS = /[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/g;

export function stripControlCharacters(value: string): string {
  return value.replace(CONTROL_CHARS, '');
}

/** Trims, strips control characters, and caps length before a string reaches the network layer. */
export function sanitizeUserInput(value: string, maxLength = 500): string {
  return stripControlCharacters(value.trim()).slice(0, maxLength);
}

/** Escapes HTML-significant characters, for the rare case text is rendered outside of JSX (e.g. document titles). */
export function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

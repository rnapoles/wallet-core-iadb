import { z } from 'zod';

/**
 * Single source of truth for environment configuration.
 * Vite only exposes variables prefixed with VITE_ to client code, and only
 * those variables should ever hold non-secret, publishable values — this
 * is a browser bundle, so nothing read here can be treated as confidential.
 * Validating eagerly means a misconfigured deployment fails fast at build
 * time instead of producing confusing runtime errors.
 */
 
const envSchema = z.object({
  VITE_API_BASE_URL: z.union([ z.string().url(), z.null(), z.literal(""), z.string().regex(/\/(([a-z][a-z0-9-]*)+\/?)*/)]),
  VITE_API_VERSION:  z.string().regex(/^(?:v(\d+))?$/).nullable(),
  VITE_API_TIMEOUT_MS: z.coerce.number().int().positive().default(15000),
  VITE_APP_NAME: z.string().min(1).default('WalletSystem'),
  VITE_ENABLE_API_LOGGING: z
    .string()
    .optional()
    .transform((value) => value === 'true'),
});

type Env = z.infer<typeof envSchema>;

function loadEnv(): Env {
  const parsed = envSchema.safeParse(import.meta.env);
  if (!parsed.success) {
    const details = parsed.error.issues
      .map((issue) => `- ${issue.path.join('.')}: ${issue.message}`)
      .join('\n');
    throw new Error(
      `Invalid environment configuration. Check your .env file against .env.example:\n${details}`,
    );
  }
  return parsed.data;
}

export const env: Env = loadEnv();

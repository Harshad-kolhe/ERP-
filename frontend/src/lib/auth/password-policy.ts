import { z } from 'zod';

/**
 * The password rules, mirroring the API's.
 *
 * A mirror, not the authority: it exists so a weak password fails before a round
 * trip. The server re-checks and wins any disagreement.
 *
 * Kept even though nothing renders a registration form today. It was the one part
 * of that form worth keeping — the rules were transcribed from the API and are
 * expensive to re-derive — and the day `POST /auth/register` exists the form is
 * rebuilt against a policy that was never lost. See `app/(auth)/register/page.tsx`
 * for why the form itself went.
 */
export const passwordPolicy = z
  .string()
  .min(12, 'Use at least 12 characters.')
  .regex(/[A-Z]/, 'Include an uppercase letter.')
  .regex(/[a-z]/, 'Include a lowercase letter.')
  .regex(/[0-9]/, 'Include a digit.')
  .regex(/[^A-Za-z0-9]/, 'Include a symbol.');

/** Said in one line, for a hint under a password box. */
export const PASSWORD_RULES_HINT =
  'At least 12 characters, with an uppercase letter, a lowercase letter, a digit and a symbol.';

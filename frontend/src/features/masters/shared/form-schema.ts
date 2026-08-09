import { z } from 'zod';

/**
 * The client half of the validation rules, mirroring `MasterValidatorExtensions`
 * on the server.
 *
 * A mirror, not the authority. These exist so a typo is caught before a round
 * trip; the server re-checks everything and wins any disagreement, and
 * `useApiForm` puts its per-field messages back under the right inputs. Keeping
 * them here rather than in five form files is what stops the five from disagreeing
 * about how long an email address may be.
 *
 * Everything is a string, because inputs produce strings. Coercing to a number in
 * the schema turns a half-typed "1." into NaN and an empty box into 0, so a field
 * nobody touched would arrive at the server as a real value.
 */

/** Optional free text with a length ceiling. */
export function text(maxLength: number, label: string) {
  return z.string().max(maxLength, `${label} must be ${maxLength} characters or fewer.`);
}

/** Text that must be filled in. */
export function requiredText(maxLength: number, label: string) {
  return z
    .string()
    .trim()
    .min(1, `${label} is required.`)
    .max(maxLength, `${label} must be ${maxLength} characters or fewer.`);
}

/**
 * Deliberately permissive: something, an @, something, a dot, something. Stricter
 * patterns reject addresses that work, and the only way to know an address is real
 * is to send to it. This catches a phone number typed into the email box.
 */
export function email(label: string) {
  return optionalPattern(
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    `${label} is not a valid email address.`,
  ).refine((value) => value.trim().length <= 150, { message: `${label} must be 150 characters or fewer.` });
}

/** Five letters, four digits, one letter. */
export function pan() {
  return optionalPattern(
    /^[A-Za-z]{5}[0-9]{4}[A-Za-z]$/,
    'PAN must be 10 characters, e.g. AAAPA1234A.',
  );
}

/** State code, PAN, entity number, Z, check character. */
export function gstin(label = 'GST number') {
  return optionalPattern(
    /^[0-9]{2}[A-Za-z]{5}[0-9]{4}[A-Za-z][0-9A-Za-z][Zz][0-9A-Za-z]$/,
    `${label} must be 15 characters, e.g. 27AAAPA1234A1Z5.`,
  );
}

/** Four letters, a zero, six alphanumerics. */
export function ifsc() {
  return optionalPattern(
    /^[A-Za-z]{4}0[A-Za-z0-9]{6}$/,
    'IFSC must be 11 characters, e.g. HDFC0001234.',
  );
}

export function swift() {
  return optionalPattern(
    /^[A-Za-z]{6}[A-Za-z0-9]{2}([A-Za-z0-9]{3})?$/,
    'SWIFT must be 8 or 11 characters.',
  );
}

export function aadhaar() {
  return optionalPattern(/^[0-9]{12}$/, 'Aadhar card no. must be 12 digits.');
}

export function cin() {
  return optionalPattern(/^[A-Za-z0-9]{21}$/, 'CIN must be 21 characters.');
}

/** A percentage. Bounded because the usual mistake here is entering the tax amount. */
export function taxRate(label: string) {
  return numberInRange(0, 100, label, `${label} must be a percentage between 0 and 100.`);
}

/** A money amount that cannot be negative. */
export function money(label: string) {
  return numberInRange(0, 9_999_999_999.99, label);
}

/** A quantity or measurement that cannot be negative. */
export function quantity(label: string, max = 9_999_999.9999) {
  return numberInRange(0, max, label);
}

/** A whole number that cannot be negative. */
export function wholeNumber(label: string, max = 1_000_000_000) {
  return numberInRange(0, max, label).refine(
    (value) => value.trim() === '' || Number.isInteger(Number(value)),
    { message: `${label} must be a whole number.` },
  );
}

/** A code chosen from a server-held list. Bounded only — the server owns what is valid. */
export function code(maxLength = 50) {
  return z.string().max(maxLength);
}

function numberInRange(min: number, max: number, label: string, rangeMessage?: string) {
  return z
    .string()
    .refine((value) => value.trim() === '' || Number.isFinite(Number(value)), {
      message: `${label} must be a number.`,
    })
    .refine((value) => value.trim() === '' || Number(value) >= min, {
      message: rangeMessage ?? `${label} cannot be negative.`,
    })
    .refine((value) => value.trim() === '' || Number(value) <= max, {
      message: rangeMessage ?? `${label} is too large.`,
    });
}

/**
 * A format rule that only applies when the box has something in it.
 *
 * Most fields on a master record are optional, and "not supplied" is not the same
 * as "supplied and wrong" — only the required check decides whether absence is
 * allowed, so every format rule here has to let blank through.
 */
function optionalPattern(pattern: RegExp, message: string) {
  return z.string().refine((value) => value.trim() === '' || pattern.test(value.trim()), { message });
}

/**
 * Translation between the API's `filter=` string and the `{ field: value }` shape
 * the two filter surfaces work in.
 *
 * There are two of those surfaces — the per-column row in the grid header, and
 * the filters panel above it — and they share one query string. Keeping the
 * translation here is what lets them coexist: each owns a set of fields and
 * rewrites only its own terms, so opening the panel cannot silently drop what
 * somebody typed into a column box.
 *
 * The wire format is `field:op:value`, semicolon-separated, parsed server-side by
 * `FilterTerm.Parse`. A field the server has not declared on its `QueryMap` is
 * answered 400 rather than ignored, which is the behaviour worth having: a filter
 * that quietly did nothing would show unfiltered data to someone who believes it
 * is filtered.
 */

/** The operators `FilterOperator` declares on the server. */
export type FilterOperator = 'contains' | 'eq' | 'neq' | 'startswith' | 'gt' | 'gte' | 'lt' | 'lte';

export interface FilterTerm {
  field: string;
  operator: FilterOperator;
  value: string;
}

/** `a:contains:x;b:eq:y` → the terms, in order. Malformed terms are dropped. */
export function parseTerms(filter: string | null | undefined): FilterTerm[] {
  if (!filter) return [];

  const terms: FilterTerm[] = [];

  for (const raw of filter.split(';')) {
    const first = raw.indexOf(':');
    if (first < 0) continue;

    const second = raw.indexOf(':', first + 1);
    if (second < 0) continue;

    const field = raw.slice(0, first).trim();
    const operator = raw.slice(first + 1, second).trim() as FilterOperator;

    // The value keeps any further colons — timestamps contain them.
    const value = raw.slice(second + 1);

    if (field && value) terms.push({ field, operator, value });
  }

  return terms;
}

export function serializeTerms(terms: readonly FilterTerm[]): string | null {
  const parts = terms
    .filter((term) => term.value.trim() !== '')
    .map((term) => `${term.field}:${term.operator}:${sanitize(term.value)}`);

  return parts.length ? parts.join(';') : null;
}

/**
 * Replaces one surface's terms while leaving every other field untouched.
 *
 * `owned` is the set of fields the caller is responsible for. Anything in the
 * current string outside that set is carried through unchanged — that is the
 * whole point, and the reason this is not a plain concatenation.
 */
export function replaceOwnedTerms(
  current: string | null | undefined,
  owned: Iterable<string>,
  next: readonly FilterTerm[],
): string | null {
  const ownedFields = new Set(owned);

  const retained = parseTerms(current).filter((term) => !ownedFields.has(term.field));

  return serializeTerms([...retained, ...next]);
}

/** The subset of a filter string belonging to the given fields, as a value map. */
export function valuesFor(
  current: string | null | undefined,
  owned: Iterable<string>,
): Record<string, string> {
  const ownedFields = new Set(owned);
  const values: Record<string, string> = {};

  for (const term of parseTerms(current)) {
    if (ownedFields.has(term.field)) values[term.field] = term.value;
  }

  return values;
}

/** How many of the given fields are actually filtering right now. */
export function countActive(values: Record<string, string>): number {
  return Object.values(values).filter((value) => value.trim() !== '').length;
}

/**
 * Colons and semicolons are the separators, so a value containing one would parse
 * as extra terms. Stripping beats escaping here: no field these screens filter on
 * holds text where either character carries meaning, and a half-applied filter is
 * worse than a slightly widened one.
 */
function sanitize(value: string): string {
  return value.trim().replaceAll(/[;:]/g, ' ');
}

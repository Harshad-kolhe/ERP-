import { describe, expect, it } from 'vitest';

import { countActive, parseTerms, replaceOwnedTerms, serializeTerms, valuesFor } from './filter-terms';

/**
 * The one piece of logic three filter surfaces depend on.
 *
 * The column row, the filters panel and the status chips all write into a single
 * `filter=` string, and the only thing stopping each from erasing the others is
 * that `replaceOwnedTerms` rewrites strictly its own fields. That is not
 * observable by reading a screen — it shows up as a filter that silently vanishes
 * — so it is the part worth a test.
 */
describe('parseTerms', () => {
  it('keeps colons inside the value', () => {
    // Timestamps contain them, and splitting on every colon would truncate one.
    expect(parseTerms('createdAt:gte:2026-08-09T10:30:00Z')).toEqual([
      { field: 'createdAt', operator: 'gte', value: '2026-08-09T10:30:00Z' },
    ]);
  });

  it('drops malformed terms rather than guessing at them', () => {
    expect(parseTerms('status;nonsense;name:eq:ACME')).toEqual([
      { field: 'name', operator: 'eq', value: 'ACME' },
    ]);
  });

  it('treats absent and empty as no filter', () => {
    expect(parseTerms(null)).toEqual([]);
    expect(parseTerms('')).toEqual([]);
  });
});

describe('serializeTerms', () => {
  it('is null when nothing is filtering, so the key leaves the URL', () => {
    expect(serializeTerms([])).toBeNull();
    expect(serializeTerms([{ field: 'name', operator: 'contains', value: '  ' }])).toBeNull();
  });

  it('strips the separators out of values without leaving stray whitespace', () => {
    // The separators have to go or the value parses back as extra terms. What
    // replaces them must then be collapsed: a doubled or trailing space makes the
    // resulting `contains` match nothing, which looks like an empty table.
    expect(serializeTerms([{ field: 'name', operator: 'contains', value: 'ACME: Ltd;' }])).toBe(
      'name:contains:ACME Ltd',
    );
  });
});

describe('replaceOwnedTerms', () => {
  it('leaves another surface’s terms alone', () => {
    const current = 'status:eq:Approved;name:contains:pump';

    // The panel owns `name` and rewrites it; the chip's `status` must survive.
    expect(
      replaceOwnedTerms(current, ['name'], [{ field: 'name', operator: 'contains', value: 'valve' }]),
    ).toBe('status:eq:Approved;name:contains:valve');
  });

  it('clears only what it owns', () => {
    expect(replaceOwnedTerms('status:eq:Hold;name:contains:pump', ['name'], [])).toBe(
      'status:eq:Hold',
    );
  });

  it('returns null once the last term goes, so the URL key is removed', () => {
    expect(replaceOwnedTerms('name:contains:pump', ['name'], [])).toBeNull();
  });
});

describe('valuesFor / countActive', () => {
  it('reads back only the requested fields', () => {
    const current = 'status:eq:Approved;name:contains:pump';

    expect(valuesFor(current, ['name'])).toEqual({ name: 'pump' });
    expect(valuesFor(current, ['status', 'name'])).toEqual({
      status: 'Approved',
      name: 'pump',
    });
  });

  it('does not count blanks as applied filters', () => {
    // The badge saying "2 applied" over an unfiltered grid is the failure here.
    expect(countActive({ a: 'x', b: '', c: '   ' })).toBe(1);
  });
});

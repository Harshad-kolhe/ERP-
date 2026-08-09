'use client';

import { useQuery } from '@tanstack/react-query';

import { apiFetch } from '@/lib/api/fetcher';

export interface LookupOption {
  code: string;
  name: string;
}

export type LookupSet = Record<string, LookupOption[]>;

/**
 * Loads the option lists a form needs.
 *
 * This is the only place dropdown options come from. Nothing in this app holds a
 * list of currencies, supplier types or units of measure — the legacy system had
 * those written into its JavaScript, so adding a payment term meant a deployment
 * and the screens quietly disagreed with each other about what was valid.
 *
 * One request per form rather than one per field: a supplier form needs six lists,
 * and six round trips is how a form renders before its dropdowns do.
 *
 * Cached for the session. These change when somebody edits reference data, which
 * is rare enough that a stale dropdown for one navigation is a better trade than
 * re-fetching the same forty rows on every screen.
 */
export function useLookups(types: readonly string[]) {
  // Sorted so ['uom','currency'] and ['currency','uom'] share one cache entry
  // instead of fetching the same data twice.
  const key = [...types].sort().join(',');

  const query = useQuery({
    queryKey: ['masters', 'lookups', key],
    queryFn: () => apiFetch<{ lookups: LookupSet }>(`/masters/lookups?types=${encodeURIComponent(key)}`),
    staleTime: 5 * 60 * 1000,
    enabled: types.length > 0,
  });

  return {
    /** Empty until loaded, so a form can render its inputs before the options arrive. */
    lookups: query.data?.lookups ?? EMPTY,
    /**
     * A failed request also yields an empty set, so a caller that only watched
     * `isPending` left every dropdown on the form sitting at "Loading…" forever.
     * Callers use this to say the options are unavailable instead.
     */
    isError: query.isError,
  };
}

const EMPTY: LookupSet = {};

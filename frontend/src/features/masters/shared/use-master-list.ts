'use client';

import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '@/lib/api/fetcher';
import type { PagedResult } from '@/lib/api/types';

/**
 * Fetches one page of any master list.
 *
 * One hook rather than one per master: every list endpoint under `/masters`
 * takes the same page/sort/search/filter contract and returns the same
 * `PagedResult<T>`, so a per-resource hook would differ only in two string
 * literals. `usePartsList` predates this and stays as it is; parts additionally
 * needs detail and mutation keys of its own.
 *
 * The query key includes the full query string, so every distinct combination of
 * page, sort, filter and search is cached separately and going back to a previous
 * page is instant without re-fetching.
 */
export function useMasterList<TRow>(resource: string, queryString: string) {
  return useQuery({
    queryKey: ['masters', resource, 'list', queryString],
    queryFn: () => apiFetch<PagedResult<TRow>>(`/masters/${resource}?${queryString}`),
    placeholderData: (previous) => previous,
  });
}

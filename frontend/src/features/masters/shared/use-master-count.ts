'use client';

import { useQuery } from '@tanstack/react-query';

import { apiFetch } from '@/lib/api/fetcher';
import type { PagedResult } from '@/lib/api/types';

/**
 * How many records a master holds, optionally narrowed by a filter.
 *
 * Asks for one row and reads `totalCount` off the paged envelope, which the list
 * endpoint already computes for the pager — so this is a count query the database
 * was going to run anyway, not a new kind of question. No endpoint per master, and
 * nothing that could return a different number from the grid beneath it.
 *
 * Cached for a minute: a header that re-counts the table on every navigation
 * costs a query per screen to tell somebody something that changes hourly.
 */
export function useMasterCount(resource: string, filter?: string) {
  const query = useQuery({
    queryKey: ['masters', resource, 'count', filter ?? ''],
    queryFn: () => {
      const params = new URLSearchParams({ page: '1', pageSize: '1' });
      if (filter) params.set('filter', filter);

      return apiFetch<PagedResult<unknown>>(`/masters/${resource}?${params}`);
    },
    staleTime: 60_000,
  });

  return {
    count: query.data?.totalCount ?? null,
    isLoading: query.isPending,
    /**
     * Distinguishes "counting" from "the count failed". Both render an em dash —
     * never a confident 0 — but only one of them is going to resolve, and the
     * header says which.
     */
    isError: query.isError,
  };
}

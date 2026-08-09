'use client';

import { useQueries } from '@tanstack/react-query';

import { apiFetch } from '@/lib/api/fetcher';
import type { PagedResult, PartListItem, PartStatus } from '@/lib/api/types';

/**
 * Counts for the dashboard, read through the ordinary list endpoint.
 *
 * `pageSize=1` fetches one row and reads `totalCount` from the paged envelope, so
 * a count costs a `COUNT(*)` and a single row rather than a bespoke summary
 * endpoint per tile. When a block needs more than a count it gets a real endpoint;
 * until then this keeps the dashboard from becoming a reason to add API surface.
 */
const STATUSES: PartStatus[] = ['Draft', 'PendingApproval', 'Approved', 'Inactive'];

export interface StatusCount {
  status: PartStatus;
  label: string;
  count: number;
  /** The filtered list this number opens. A count you cannot act on is decoration. */
  href: string;
}

const LABELS: Record<PartStatus, string> = {
  Draft: 'Draft',
  PendingApproval: 'Awaiting approval',
  Approved: 'Approved',
  Inactive: 'Inactive',
};

export function usePartCounts() {
  const queries = useQueries({
    queries: STATUSES.map((status) => ({
      queryKey: ['dashboard', 'parts', status] as const,
      queryFn: () =>
        apiFetch<PagedResult<PartListItem>>(
          `/masters/parts?pageSize=1&filter=status:eq:${status}`,
        ),
      staleTime: 60_000,
    })),
  });

  const isLoading = queries.some((query) => query.isLoading);
  const isError = queries.some((query) => query.isError);

  const counts: StatusCount[] = STATUSES.map((status, index) => ({
    status,
    label: LABELS[status],
    count: queries[index]?.data?.totalCount ?? 0,
    href: `/masters/parts?filter=status:eq:${status}`,
  }));

  return {
    counts,
    total: counts.reduce((sum, entry) => sum + entry.count, 0),
    isLoading,
    isError,
  };
}

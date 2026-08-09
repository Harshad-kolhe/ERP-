'use client';

import { useQueries } from '@tanstack/react-query';

import { apiFetch } from '@/lib/api/fetcher';
import type { MasterStatus, PagedResult } from '@/lib/api/types';

import { useMasterCount } from './use-master-count';

/**
 * How many records a master holds in each state of the approval lifecycle.
 *
 * Read through the ordinary list endpoint: `pageSize=1` fetches one row and takes
 * `totalCount` off the paged envelope, so a count costs a `COUNT(*)` and a single
 * row rather than a summary endpoint per master. When a screen needs more than a
 * count it gets a real endpoint; until then this is what keeps six status bands
 * from becoming six additions to the API.
 *
 * Only for masters the API will accept `status:eq:` on — parts, suppliers,
 * customers, employees. Roles and business units have no lifecycle, only an active
 * flag, and their list endpoints answer 400 to a status filter.
 *
 * `MasterStatus` rather than `PartStatus`: the two are separate wire contracts that
 * list the same five members today, and this hook is shared, so it names the one
 * that is not about parts.
 */
const STATUSES: MasterStatus[] = ['Draft', 'PendingApproval', 'Approved', 'Rejected', 'Hold'];

/**
 * Deliberately not `STATUS_LABEL` from `master-columns`, which says "Pending
 * approval" — that is the wording of a pill sitting inside a row. A band above the
 * grid is a queue to clear, and "Awaiting approval" is what a queue is called.
 */
const LABELS: Record<MasterStatus, string> = {
  Draft: 'Draft',
  PendingApproval: 'Awaiting approval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Hold: 'On hold',
};

export interface StatusCount {
  status: MasterStatus;
  label: string;
  count: number;
  /** The filtered list this number opens. A count you cannot act on is decoration. */
  href: string;
}

export function useStatusCounts(resource: string) {
  const queries = useQueries({
    queries: STATUSES.map((status) => ({
      // Under `['masters', resource, …]` so that anything invalidating the master
      // refreshes these too — an import that lands 20 rows must not leave a band
      // above the grid still reading zero.
      queryKey: ['masters', resource, 'status-count', status] as const,
      queryFn: () =>
        apiFetch<PagedResult<unknown>>(`/masters/${resource}?pageSize=1&filter=status:eq:${status}`),
      staleTime: 60_000,
    })),
  });

  /*
   * The master's own total, asked for rather than added up.
   *
   * `total` used to be the five status counts summed, which is only the right
   * answer while those five are the only states that exist. A record with a status
   * this list does not name — a sixth member, or a null carried in from the legacy
   * data — would be missing from it, and "all records" is the one figure that must
   * never be quietly short. `useMasterCount` already asks the exact question, so
   * this is its query rather than a sixth one of ours.
   */
  const totalQuery = useMasterCount(resource);

  const counts: StatusCount[] = STATUSES.map((status, index) => ({
    status,
    label: LABELS[status],
    count: queries[index]?.data?.totalCount ?? 0,
    href: `/masters/${resource}?filter=status:eq:${status}`,
  }));

  return {
    counts,
    total: totalQuery.count ?? 0,
    isLoading: queries.some((query) => query.isLoading) || totalQuery.isLoading,
    /**
     * Every count falls back to 0, so a caller that ignores this renders a
     * confident, wrong, reassuring "nothing here" over a dead API.
     */
    isError: queries.some((query) => query.isError) || totalQuery.isError,
  };
}

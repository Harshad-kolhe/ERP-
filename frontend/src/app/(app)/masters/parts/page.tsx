import { Suspense } from 'react';

import { PartStatusChips } from '@/features/masters/parts/part-status-chips';
import { PartsPageHeader } from '@/features/masters/parts/parts-page-header';
import { PartsTable } from '@/features/masters/parts/parts-table';

export const metadata = { title: 'Part Master · ERP' };

/**
 * Part Master, in the approved prototype's layout: an identity band carrying the
 * counts and the page's actions, a status band whose numbers are also its filters,
 * and the grid below.
 *
 * The generic `PageHeader` is deliberately not used here. It gives a title and a
 * sentence; this page needs the record count, a seven-item action set and a status
 * breakdown, and squeezing those into the shared component would push every other
 * master's header around to serve one screen.
 */
export default function PartsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      {/* useSearchParams and the count queries both need a boundary during prerender. */}
      <Suspense fallback={<div className="border-border h-[57px] shrink-0 border-b" />}>
        <PartsPageHeader />
      </Suspense>

      <div className="flex min-h-0 flex-1 flex-col gap-3 p-4">
        <Suspense fallback={<div className="h-[52px]" />}>
          <PartStatusChips />
        </Suspense>

        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <PartsTable />
        </Suspense>
      </div>
    </div>
  );
}

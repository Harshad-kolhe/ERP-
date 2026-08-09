import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { MasterPageHeader } from '@/features/masters/shared/master-page-header';
import { ParentPartsTable } from '@/features/masters/parent-parts/parent-parts-table';

export const metadata = { title: 'Parent Part Master · ERP' };

export default function ParentPartsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      {/* The header counts through the API, so it needs a boundary during prerender. */}
      <Suspense fallback={<div className="border-line h-[69px] shrink-0 border-b" />}>
        <MasterPageHeader
          icon="parentPart"
          title="Parent Part Master"
          resource="parent-parts"
          stats={[
            { label: 'parent parts' },
        { label: 'inactive', filter: 'isActive:eq:false' },
          ]}
          actions={
            <Can permission="masters.parentpart.create">
              <Link
                href="/masters/parent-parts/new"
                className="bg-primary hover:bg-primary/90 text-primary-foreground inline-flex h-8 items-center gap-1.5 rounded-lg px-3.5 text-[13px] font-semibold shadow-sm"
              >
                <span aria-hidden="true" className="-mt-px text-base leading-none">
                  +
                </span>
                New Parent Part
              </Link>
            </Can>
          }
        />
      </Suspense>

      <div className="flex min-h-0 flex-1 flex-col p-4">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <ParentPartsTable />
        </Suspense>
      </div>
    </div>
  );
}

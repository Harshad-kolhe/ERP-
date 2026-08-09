import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { MasterPageHeader } from '@/features/masters/shared/master-page-header';
import { BusinessUnitsTable } from '@/features/masters/business-units/business-units-table';

export const metadata = { title: 'Business Units · ERP' };

export default function BusinessUnitsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      {/* The header counts through the API, so it needs a boundary during prerender. */}
      <Suspense fallback={<div className="border-line h-[69px] shrink-0 border-b" />}>
        <MasterPageHeader
          icon="businessUnit"
          title="Business Units"
          resource="business-units"
          stats={[
            { label: 'business units' },
        { label: 'inactive', filter: 'isActive:eq:false' },
          ]}
          actions={
            <Can permission="masters.businessunit.create">
              <Link
                href="/masters/business-units/new"
                className="bg-primary hover:bg-primary/90 text-primary-foreground inline-flex h-8 items-center gap-1.5 rounded-lg px-3.5 text-[13px] font-semibold shadow-sm"
              >
                <span aria-hidden="true" className="-mt-px text-base leading-none">
                  +
                </span>
                New Business Unit
              </Link>
            </Can>
          }
        />
      </Suspense>

      <div className="flex min-h-0 flex-1 flex-col p-4">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <BusinessUnitsTable />
        </Suspense>
      </div>
    </div>
  );
}

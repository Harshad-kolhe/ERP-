import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { BusinessUnitsTable } from '@/features/masters/business-units/business-units-table';

export const metadata = { title: 'Business units · ERP' };

export default function BusinessUnitsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Business units"
        description="The tenancy boundary every other record is scoped to. This list is not itself scoped, so it shows every unit."
        actions={
          <Can permission="masters.businessunit.create">
            <Button size="sm" asChild>
              <Link href="/masters/business-units/new">New business unit</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <BusinessUnitsTable />
        </Suspense>
      </div>
    </div>
  );
}

import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { ParentPartsTable } from '@/features/masters/parent-parts/parent-parts-table';

export const metadata = { title: 'Parent parts · ERP' };

export default function ParentPartsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Parent parts"
        description="Parts that are built from other parts. One row per build; weight and amount are rolled up from its component lines."
        actions={
          <Can permission="masters.parentpart.create">
            <Button size="sm" asChild>
              <Link href="/masters/parent-parts/new">New parent part</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <ParentPartsTable />
        </Suspense>
      </div>
    </div>
  );
}

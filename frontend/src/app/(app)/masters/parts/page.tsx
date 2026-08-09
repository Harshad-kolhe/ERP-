import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { PartsTable } from '@/features/masters/parts/parts-table';

export const metadata = { title: 'Parts · ERP' };

export default function PartsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Parts"
        description="Server-paged. Filtering and sorting run in the database; the browser only ever holds one page."
        actions={
          // Rendered only for users holding masters.part.create. The endpoint behind
          // it enforces the same permission, so this is about not offering an action
          // that would fail — not about security.
          <Can permission="masters.part.create">
            <Button size="sm" asChild>
              <Link href="/masters/parts/new">New part</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <PartsTable />
        </Suspense>
      </div>
    </div>
  );
}

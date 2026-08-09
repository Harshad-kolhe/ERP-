import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { SuppliersTable } from '@/features/masters/suppliers/suppliers-table';

export const metadata = { title: 'Suppliers · ERP' };

export default function SuppliersPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Suppliers"
        description="Server-paged. Filtering and sorting run in the database; the browser only ever holds one page."
        actions={
          <Can permission="masters.supplier.create">
            <Button size="sm" asChild>
              <Link href="/masters/suppliers/new">New supplier</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <SuppliersTable />
        </Suspense>
      </div>
    </div>
  );
}

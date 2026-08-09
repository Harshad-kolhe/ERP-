import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { CustomersTable } from '@/features/masters/customers/customers-table';

export const metadata = { title: 'Customers · ERP' };

export default function CustomersPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Customers"
        description="Server-paged. Filtering and sorting run in the database; the browser only ever holds one page."
        actions={
          <Can permission="masters.customer.create">
            <Button size="sm" asChild>
              <Link href="/masters/customers/new">New customer</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <CustomersTable />
        </Suspense>
      </div>
    </div>
  );
}

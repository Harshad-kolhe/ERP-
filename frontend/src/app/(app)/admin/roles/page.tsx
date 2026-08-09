import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { RolesTable } from '@/features/admin/roles/roles-table';

export const metadata = { title: 'Roles · ERP' };

export default function RolesPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Roles"
        description="What each role may do. This is the only place the permission mapping is made — nothing is hardcoded in the application."
        actions={
          <Can permission="admin.role.create">
            <Button size="sm" asChild>
              <Link href="/admin/roles/new">New role</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <RolesTable />
        </Suspense>
      </div>
    </div>
  );
}

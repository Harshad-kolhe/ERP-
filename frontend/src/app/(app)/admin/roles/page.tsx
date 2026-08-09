import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { RolesTable } from '@/features/admin/roles/roles-table';
import { GridSkeleton } from '@/features/masters/shared/master-list-screen';

// Named apart from Masters › Roles deliberately. The two are different tables
// behind different permissions, and both used to title this tab "Roles · ERP",
// so two open tabs were indistinguishable.
export const metadata = { title: 'Roles & permissions · ERP' };

export default function AdminRolesPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Roles & permissions"
        description="What each role may do. This is the only place the permission mapping is made — nothing is hardcoded in the application. Distinct from Masters › Roles, which is the reference table."
        actions={
          <Can permission="admin.role.create">
            <Button size="sm" asChild>
              <Link href="/admin/roles/new">New role</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-4">
        <Suspense fallback={<GridSkeleton />}>
          <RolesTable />
        </Suspense>
      </div>
    </div>
  );
}

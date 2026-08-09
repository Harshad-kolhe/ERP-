import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { RolesTable } from '@/features/masters/roles/roles-table';

export const metadata = { title: 'Roles · ERP' };

export default function MasterRolesPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Roles"
        description="The role reference table carried over from the legacy system. These records grant nothing — permissions are administered under Roles & permissions."
        actions={
          <Can permission="masters.role.create">
            <Button size="sm" asChild>
              <Link href="/masters/roles/new">New role</Link>
            </Button>
          </Can>
        }
      />

      {/*
        Two different things in this system are called "role", and confusing them is
        easy enough that the screen says so. This one is master data; the one that
        grants permissions lives under Administer.
      */}
      <div className="px-6 pt-4">
        <p className="bg-muted/50 text-muted-foreground rounded-md border px-3 py-2 text-[13px]">
          Looking for what a role is allowed to do?{' '}
          <Can
            permission="admin.role.read"
            fallback={<span>Ask an administrator — it lives under Administer › Roles &amp; permissions.</span>}
          >
            <Link href="/admin/roles" className="text-primary font-medium underline-offset-4 hover:underline">
              Roles &amp; permissions
            </Link>
          </Can>
        </p>
      </div>

      <div className="flex min-h-0 flex-1 flex-col p-6">
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <RolesTable />
        </Suspense>
      </div>
    </div>
  );
}

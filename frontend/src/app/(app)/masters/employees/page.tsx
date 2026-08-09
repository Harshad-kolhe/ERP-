import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
import { EmployeesTable } from '@/features/masters/employees/employees-table';

export const metadata = { title: 'Employees · ERP' };

export default function EmployeesPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Employees"
        description="People on the payroll. Sign-in accounts are managed separately under Administer › Users."
        actions={
          <Can permission="masters.employee.create">
            <Button size="sm" asChild>
              <Link href="/masters/employees/new">New employee</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <EmployeesTable />
        </Suspense>
      </div>
    </div>
  );
}

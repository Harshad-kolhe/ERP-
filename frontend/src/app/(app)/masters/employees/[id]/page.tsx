import { PageHeader } from '@/components/shell/page-header';
import { EditMasterRecord } from '@/features/masters/shared/edit-master-record';
import { EmployeeForm } from '@/features/masters/employees/employee-form';
import type { EmployeeDetail } from '@/lib/api/types';

export const metadata = { title: 'Edit employee · ERP' };

export default async function EditEmployeePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit employee"
        description="Saving checks the version you loaded, so a colleague's concurrent edit is reported rather than overwritten."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditMasterRecord<EmployeeDetail> resource="employees" id={id} noun="employee">
          {(record) => <EmployeeForm employee={record} />}
        </EditMasterRecord>
      </div>
    </div>
  );
}

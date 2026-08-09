import { PageHeader } from '@/components/shell/page-header';
import { EmployeeForm } from '@/features/masters/employees/employee-form';

export const metadata = { title: 'New employee · ERP' };

export default function NewEmployeePage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader title="New employee" description="Pay details appear only if you hold the employee payroll permission." />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <EmployeeForm />
      </div>
    </div>
  );
}

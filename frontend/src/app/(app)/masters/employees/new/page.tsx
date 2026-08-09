import { EmployeeForm } from '@/features/masters/employees/employee-form';

export const metadata = { title: 'New employee · ERP' };

export default function NewEmployeePage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex min-h-0 flex-1 flex-col">
        <EmployeeForm />
      </div>
    </div>
  );
}

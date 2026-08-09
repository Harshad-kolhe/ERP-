import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { EmployeesTable } from '@/features/masters/employees/employees-table';

export const metadata = { title: 'Employee Master · ERP' };

export default function EmployeesPage() {
  return (
    <MasterListScreen
      icon="employee"
      title="Employee Master"
      resource="employees"
      noun="Employee"
      createPermission="masters.employee.create"
      stats={[
        { label: 'employees' },
        { label: 'awaiting approval', filter: 'status:eq:PendingApproval', emphasise: true },
      ]}
    >
      <EmployeesTable />
    </MasterListScreen>
  );
}

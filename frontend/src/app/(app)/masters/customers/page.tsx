import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { CustomersTable } from '@/features/masters/customers/customers-table';

export const metadata = { title: 'Customer Master · ERP' };

export default function CustomersPage() {
  return (
    <MasterListScreen
      icon="customer"
      title="Customer Master"
      resource="customers"
      noun="Customer"
      createPermission="masters.customer.create"
      importPermission="masters.customer.import"
      statusChips
    >
      <CustomersTable />
    </MasterListScreen>
  );
}

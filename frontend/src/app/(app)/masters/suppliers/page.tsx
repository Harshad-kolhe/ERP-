import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { SuppliersTable } from '@/features/masters/suppliers/suppliers-table';

export const metadata = { title: 'Supplier Master · ERP' };

export default function SuppliersPage() {
  return (
    <MasterListScreen
      icon="supplier"
      title="Supplier Master"
      resource="suppliers"
      noun="Supplier"
      createPermission="masters.supplier.create"
      importPermission="masters.supplier.import"
      statusChips
    >
      <SuppliersTable />
    </MasterListScreen>
  );
}

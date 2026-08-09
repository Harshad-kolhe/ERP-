import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { RolesTable } from '@/features/masters/roles/roles-table';

export const metadata = { title: 'Role Master · ERP' };

export default function RolesPage() {
  return (
    <MasterListScreen
      icon="role"
      title="Role Master"
      resource="roles"
      noun="Role"
      createPermission="masters.role.create"
      stats={[
        { label: 'roles' },
        { label: 'inactive', filter: 'isActive:eq:false' },
      ]}
    >
      <RolesTable />
    </MasterListScreen>
  );
}

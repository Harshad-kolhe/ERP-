import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { ParentPartsTable } from '@/features/masters/parent-parts/parent-parts-table';

export const metadata = { title: 'Parent Part Master · ERP' };

export default function ParentPartsPage() {
  return (
    <MasterListScreen
      icon="parentPart"
      title="Parent Part Master"
      resource="parent-parts"
      noun="Parent Part"
      createPermission="masters.parentpart.create"
      stats={[
        { label: 'parent parts' },
        { label: 'inactive', filter: 'isActive:eq:false' },
      ]}
    >
      <ParentPartsTable />
    </MasterListScreen>
  );
}

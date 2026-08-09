import { PageHeader } from '@/components/shell/page-header';
import { EditMasterRecord } from '@/features/masters/shared/edit-master-record';
import { RoleMasterForm } from '@/features/masters/roles/role-master-form';
import type { RoleMasterDetail } from '@/lib/api/types';

export const metadata = { title: 'Edit role · ERP' };

export default async function EditRoleMasterPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit role"
        description="Saving checks the version you loaded, so a colleague's concurrent edit is reported rather than overwritten."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditMasterRecord<RoleMasterDetail> resource="roles" id={id} noun="role">
          {(record) => <RoleMasterForm role={record} />}
        </EditMasterRecord>
      </div>
    </div>
  );
}

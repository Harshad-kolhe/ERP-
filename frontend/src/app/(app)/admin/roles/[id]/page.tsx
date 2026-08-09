import { PageHeader } from '@/components/shell/page-header';
import { EditRole } from '@/features/admin/roles/edit-role';

export const metadata = { title: 'Edit role · ERP' };

export default async function EditRolePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit role"
        description="Changes take effect for a user at their next sign-in, because permissions are attached to the session when it is issued."
      />
      <div className="flex min-h-0 flex-1 flex-col p-6">
        <EditRole id={id} />
      </div>
    </div>
  );
}

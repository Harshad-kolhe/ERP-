import { EditRoleMaster } from '@/features/masters/roles/edit-role-master';

export const metadata = { title: 'Edit role · ERP' };

export default async function EditRoleMasterPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditRoleMaster id={id} />
    </div>
  );
}

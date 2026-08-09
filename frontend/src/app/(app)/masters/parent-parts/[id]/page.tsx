import { EditParentPart } from '@/features/masters/parent-parts/edit-parent-part';

export const metadata = { title: 'Edit parent part · ERP' };

export default async function EditParentPartPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditParentPart id={id} />
    </div>
  );
}

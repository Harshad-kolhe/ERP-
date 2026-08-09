import { EditPart } from '@/features/masters/parts/edit-part';

export const metadata = { title: 'Edit part · ERP' };

export default async function EditPartPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditPart id={id} />
    </div>
  );
}

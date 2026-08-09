import { EditHsnCode } from '@/features/masters/hsn-codes/edit-hsn-code';

export const metadata = { title: 'Edit HSN code · ERP' };

export default async function EditHsnCodePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditHsnCode id={id} />
    </div>
  );
}

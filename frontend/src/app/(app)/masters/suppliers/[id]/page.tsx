import { EditSupplier } from '@/features/masters/suppliers/edit-supplier';

export const metadata = { title: 'Edit supplier · ERP' };

export default async function EditSupplierPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditSupplier id={id} />
    </div>
  );
}

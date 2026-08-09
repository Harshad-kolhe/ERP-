import { EditBusinessUnit } from '@/features/masters/business-units/edit-business-unit';

export const metadata = { title: 'Edit business unit · ERP' };

export default async function EditBusinessUnitPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditBusinessUnit id={id} />
    </div>
  );
}

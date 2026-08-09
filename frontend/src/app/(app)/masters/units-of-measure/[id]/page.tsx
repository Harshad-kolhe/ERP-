import { EditUnitOfMeasure } from '@/features/masters/units-of-measure/edit-unit-of-measure';

export const metadata = { title: 'Edit unit · ERP' };

export default async function EditUnitOfMeasurePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditUnitOfMeasure id={id} />
    </div>
  );
}

import { EditEmployee } from '@/features/masters/employees/edit-employee';

export const metadata = { title: 'Edit employee · ERP' };

export default async function EditEmployeePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditEmployee id={id} />
    </div>
  );
}

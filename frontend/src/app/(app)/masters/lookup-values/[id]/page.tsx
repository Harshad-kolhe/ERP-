import { EditLookupValue } from '@/features/masters/lookup-values/edit-lookup-value';

export const metadata = { title: 'Edit option · ERP' };

export default async function EditLookupValuePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditLookupValue id={id} />
    </div>
  );
}

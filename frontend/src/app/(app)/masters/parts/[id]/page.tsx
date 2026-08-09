import { PageHeader } from '@/components/shell/page-header';
import { EditPart } from '@/features/masters/parts/edit-part';

export const metadata = { title: 'Edit part · ERP' };

export default async function EditPartPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit part"
        description="A part awaiting approval cannot be edited — the approver has to be looking at what they approve."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditPart id={id} />
      </div>
    </div>
  );
}

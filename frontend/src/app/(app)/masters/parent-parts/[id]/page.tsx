import { PageHeader } from '@/components/shell/page-header';
import { ParentPartForm } from '@/features/masters/parent-parts/parent-part-form';
import { EditMasterRecord } from '@/features/masters/shared/edit-master-record';
import type { ParentPartDetail } from '@/lib/api/types';

export const metadata = { title: 'Edit parent part · ERP' };

export default async function EditParentPartPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit parent part"
        description="The component list is replaced as a whole, in one transaction, and the totals are recomputed from it."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditMasterRecord<ParentPartDetail> resource="parent-parts" id={id} noun="parent part">
          {(parentPart) => <ParentPartForm parentPart={parentPart} />}
        </EditMasterRecord>
      </div>
    </div>
  );
}

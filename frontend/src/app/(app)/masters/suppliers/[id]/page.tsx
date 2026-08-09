import { PageHeader } from '@/components/shell/page-header';
import { EditMasterRecord } from '@/features/masters/shared/edit-master-record';
import { SupplierForm } from '@/features/masters/suppliers/supplier-form';
import type { SupplierDetail } from '@/lib/api/types';

export const metadata = { title: 'Edit supplier · ERP' };

export default async function EditSupplierPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit supplier"
        description="Saving checks the version you loaded, so a colleague's concurrent edit is reported rather than overwritten."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditMasterRecord<SupplierDetail> resource="suppliers" id={id} noun="supplier">
          {(supplier) => <SupplierForm supplier={supplier} />}
        </EditMasterRecord>
      </div>
    </div>
  );
}

import { PageHeader } from '@/components/shell/page-header';
import { EditMasterRecord } from '@/features/masters/shared/edit-master-record';
import { BusinessUnitForm } from '@/features/masters/business-units/business-unit-form';
import type { BusinessUnitDetail } from '@/lib/api/types';

export const metadata = { title: 'Edit business unit · ERP' };

export default async function EditBusinessUnitPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit business unit"
        description="Saving checks the version you loaded, so a colleague's concurrent edit is reported rather than overwritten."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditMasterRecord<BusinessUnitDetail> resource="business-units" id={id} noun="business unit">
          {(record) => <BusinessUnitForm unit={record} />}
        </EditMasterRecord>
      </div>
    </div>
  );
}

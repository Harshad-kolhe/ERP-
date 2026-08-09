import { PageHeader } from '@/components/shell/page-header';
import { EditMasterRecord } from '@/features/masters/shared/edit-master-record';
import { CustomerForm } from '@/features/masters/customers/customer-form';
import type { CustomerDetail } from '@/lib/api/types';

export const metadata = { title: 'Edit customer · ERP' };

export default async function EditCustomerPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Edit customer"
        description="Saving checks the version you loaded, so a colleague's concurrent edit is reported rather than overwritten."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditMasterRecord<CustomerDetail> resource="customers" id={id} noun="customer">
          {(record) => <CustomerForm customer={record} />}
        </EditMasterRecord>
      </div>
    </div>
  );
}

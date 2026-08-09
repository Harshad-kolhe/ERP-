import { PageHeader } from '@/components/shell/page-header';
import { SupplierForm } from '@/features/masters/suppliers/supplier-form';

export const metadata = { title: 'New supplier · ERP' };

export default function NewSupplierPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="New supplier"
        description="Only the code and name are required. The rest can be filled in as it arrives."
      />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <SupplierForm />
      </div>
    </div>
  );
}

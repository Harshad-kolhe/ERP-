import { PageHeader } from '@/components/shell/page-header';
import { CustomerForm } from '@/features/masters/customers/customer-form';

export const metadata = { title: 'New customer · ERP' };

export default function NewCustomerPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader title="New customer" description="Only the code and name are required. The rest can be filled in as it arrives." />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <CustomerForm />
      </div>
    </div>
  );
}

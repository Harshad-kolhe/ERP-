import { PageHeader } from '@/components/shell/page-header';
import { BusinessUnitForm } from '@/features/masters/business-units/business-unit-form';

export const metadata = { title: 'New business unit · ERP' };

export default function NewBusinessUnitPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader title="New business unit" description="The unit id is what every other record carries in its tenancy column." />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <BusinessUnitForm />
      </div>
    </div>
  );
}

import { PageHeader } from '@/components/shell/page-header';
import { PartForm } from '@/features/masters/parts/part-form';

export const metadata = { title: 'New part · ERP' };

export default function NewPartPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="New part"
        description="The part is created as a draft. It becomes usable on other documents once approved."
      />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <PartForm />
      </div>
    </div>
  );
}

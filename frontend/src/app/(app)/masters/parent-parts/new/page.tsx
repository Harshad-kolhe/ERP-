import { PageHeader } from '@/components/shell/page-header';
import { ParentPartForm } from '@/features/masters/parent-parts/parent-part-form';

export const metadata = { title: 'New parent part · ERP' };

export default function NewParentPartPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="New parent part"
        description="Choose the part being built and list what goes into it. A part may have one build."
      />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <ParentPartForm />
      </div>
    </div>
  );
}

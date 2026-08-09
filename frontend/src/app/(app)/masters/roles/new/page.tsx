import { PageHeader } from '@/components/shell/page-header';
import { RoleMasterForm } from '@/features/masters/roles/role-master-form';

export const metadata = { title: 'New role · ERP' };

export default function NewRoleMasterPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader title="New role" description="The legacy role master. It grants no permissions." />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <RoleMasterForm />
      </div>
    </div>
  );
}

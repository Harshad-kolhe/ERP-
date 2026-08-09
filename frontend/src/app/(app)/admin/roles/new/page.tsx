import { PageHeader } from '@/components/shell/page-header';
import { RoleForm } from '@/features/admin/roles/role-form';

export const metadata = { title: 'New role · ERP' };

export default function NewRolePage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader title="New role" description="Name the role, then choose what it may do." />
      <div className="flex min-h-0 flex-1 flex-col p-6">
        <RoleForm />
      </div>
    </div>
  );
}

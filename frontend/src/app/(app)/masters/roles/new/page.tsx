import { RoleMasterForm } from '@/features/masters/roles/role-master-form';

export const metadata = { title: 'New role · ERP' };

export default function NewRoleMasterPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <RoleMasterForm />
    </div>
  );
}

import { ParentPartForm } from '@/features/masters/parent-parts/parent-part-form';

export const metadata = { title: 'New parent part · ERP' };

export default function NewParentPartPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex min-h-0 flex-1 flex-col">
        <ParentPartForm />
      </div>
    </div>
  );
}

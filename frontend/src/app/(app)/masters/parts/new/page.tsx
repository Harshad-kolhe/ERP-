import { PartForm } from '@/features/masters/parts/part-form';

export const metadata = { title: 'New part · ERP' };

export default function NewPartPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PartForm />
    </div>
  );
}

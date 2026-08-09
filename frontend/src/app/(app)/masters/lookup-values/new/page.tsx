import { LookupValueForm } from '@/features/masters/lookup-values/lookup-value-form';

export const metadata = { title: 'New option · ERP' };

export default function NewLookupValuePage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <LookupValueForm />
    </div>
  );
}

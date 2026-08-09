import { HsnCodeForm } from '@/features/masters/hsn-codes/hsn-code-form';

export const metadata = { title: 'New HSN code · ERP' };

export default function NewHsnCodePage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <HsnCodeForm />
    </div>
  );
}

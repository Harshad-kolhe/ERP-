import { SupplierForm } from '@/features/masters/suppliers/supplier-form';

export const metadata = { title: 'New supplier · ERP' };

export default function NewSupplierPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <SupplierForm />
    </div>
  );
}

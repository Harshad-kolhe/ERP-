import { CustomerForm } from '@/features/masters/customers/customer-form';

export const metadata = { title: 'New customer · ERP' };

export default function NewCustomerPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <CustomerForm />
    </div>
  );
}

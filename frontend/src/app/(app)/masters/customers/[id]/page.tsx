import { EditCustomer } from '@/features/masters/customers/edit-customer';

export const metadata = { title: 'Edit customer · ERP' };

export default async function EditCustomerPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditCustomer id={id} />
    </div>
  );
}

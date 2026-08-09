import { BusinessUnitForm } from '@/features/masters/business-units/business-unit-form';

export const metadata = { title: 'New business unit · ERP' };

export default function NewBusinessUnitPage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <BusinessUnitForm />
    </div>
  );
}

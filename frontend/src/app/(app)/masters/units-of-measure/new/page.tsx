import { UnitOfMeasureForm } from '@/features/masters/units-of-measure/unit-of-measure-form';

export const metadata = { title: 'New unit · ERP' };

export default function NewUnitOfMeasurePage() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <UnitOfMeasureForm />
    </div>
  );
}

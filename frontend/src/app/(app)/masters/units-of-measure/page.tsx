import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { UnitsOfMeasureTable } from '@/features/masters/units-of-measure/units-of-measure-table';

export const metadata = { title: 'Units of Measure · ERP' };

export default function UnitsOfMeasurePage() {
  return (
    <MasterListScreen
      icon="unitOfMeasure"
      title="Units of Measure"
      resource="units-of-measure"
      noun="Unit"
      createPermission="masters.referencedata.create"
      stats={[{ label: 'units' }, { label: 'retired', filter: 'isActive:eq:false' }]}
    >
      <UnitsOfMeasureTable />
    </MasterListScreen>
  );
}

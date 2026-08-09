import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { BusinessUnitsTable } from '@/features/masters/business-units/business-units-table';

export const metadata = { title: 'Business Units · ERP' };

export default function BusinessUnitsPage() {
  return (
    <MasterListScreen
      icon="businessUnit"
      title="Business Units"
      resource="business-units"
      noun="Business Unit"
      createPermission="masters.businessunit.create"
      importPermission="masters.businessunit.import"
      stats={[
        { label: 'business units' },
        { label: 'inactive', filter: 'isActive:eq:false' },
      ]}
    >
      <BusinessUnitsTable />
    </MasterListScreen>
  );
}

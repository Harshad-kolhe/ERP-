import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { LookupValuesTable } from '@/features/masters/lookup-values/lookup-values-table';

export const metadata = { title: 'Reference Data · ERP' };

export default function LookupValuesPage() {
  return (
    <MasterListScreen
      icon="lookupValue"
      title="Reference Data"
      resource="lookup-values"
      noun="Option"
      createPermission="masters.referencedata.create"
      stats={[{ label: 'options' }, { label: 'retired', filter: 'isActive:eq:false' }]}
    >
      <LookupValuesTable />
    </MasterListScreen>
  );
}

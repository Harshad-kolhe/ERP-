import { MasterListScreen } from '@/features/masters/shared/master-list-screen';
import { HsnCodesTable } from '@/features/masters/hsn-codes/hsn-codes-table';

export const metadata = { title: 'HSN Codes · ERP' };

export default function HsnCodesPage() {
  return (
    <MasterListScreen
      icon="hsnCode"
      title="HSN Codes"
      resource="hsn-codes"
      noun="HSN code"
      createPermission="masters.referencedata.create"
      stats={[{ label: 'codes' }, { label: 'retired', filter: 'isActive:eq:false' }]}
    >
      <HsnCodesTable />
    </MasterListScreen>
  );
}

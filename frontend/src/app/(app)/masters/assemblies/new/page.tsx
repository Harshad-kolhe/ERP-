import { NewAssemblyNodeScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { ASSEMBLY_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'New assembly · ERP' };

export default function NewASSEMBLY_SCREENPage() {
  return <NewAssemblyNodeScreen screen={ASSEMBLY_SCREEN} />;
}

import { AssemblyNodeListScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { SUB_ASSEMBLY_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'Sub-assemblies · ERP' };

export default function SUB_ASSEMBLY_SCREENPage() {
  return <AssemblyNodeListScreen screen={SUB_ASSEMBLY_SCREEN} />;
}

import { AssemblyNodeListScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { ASSEMBLY_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'Assemblies · ERP' };

export default function ASSEMBLY_SCREENPage() {
  return <AssemblyNodeListScreen screen={ASSEMBLY_SCREEN} />;
}

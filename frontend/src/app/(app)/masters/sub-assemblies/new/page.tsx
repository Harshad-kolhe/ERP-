import { NewAssemblyNodeScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { SUB_ASSEMBLY_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'New sub-assembly · ERP' };

export default function NewSUB_ASSEMBLY_SCREENPage() {
  return <NewAssemblyNodeScreen screen={SUB_ASSEMBLY_SCREEN} />;
}

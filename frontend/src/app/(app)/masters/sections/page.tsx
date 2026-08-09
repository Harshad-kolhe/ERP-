import { AssemblyNodeListScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { SECTION_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'Sections · ERP' };

export default function SECTION_SCREENPage() {
  return <AssemblyNodeListScreen screen={SECTION_SCREEN} />;
}

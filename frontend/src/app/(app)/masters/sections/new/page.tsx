import { NewAssemblyNodeScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { SECTION_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'New section · ERP' };

export default function NewSECTION_SCREENPage() {
  return <NewAssemblyNodeScreen screen={SECTION_SCREEN} />;
}

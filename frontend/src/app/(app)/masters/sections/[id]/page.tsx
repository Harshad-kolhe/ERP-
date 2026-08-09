import { EditAssemblyNodeScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { SECTION_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'Edit section · ERP' };

export default async function EditSECTION_SCREENPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return <EditAssemblyNodeScreen screen={SECTION_SCREEN} id={id} />;
}

import { EditAssemblyNodeScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { SUB_ASSEMBLY_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'Edit sub-assembly · ERP' };

export default async function EditSUB_ASSEMBLY_SCREENPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return <EditAssemblyNodeScreen screen={SUB_ASSEMBLY_SCREEN} id={id} />;
}

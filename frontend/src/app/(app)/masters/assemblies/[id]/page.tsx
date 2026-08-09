import { EditAssemblyNodeScreen } from '@/features/masters/assembly-nodes/assembly-node-screens';
import { ASSEMBLY_SCREEN } from '@/features/masters/assembly-nodes/assembly-node-level';

export const metadata = { title: 'Edit assembly · ERP' };

export default async function EditASSEMBLY_SCREENPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return <EditAssemblyNodeScreen screen={ASSEMBLY_SCREEN} id={id} />;
}

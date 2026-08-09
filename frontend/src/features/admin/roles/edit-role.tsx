'use client';

import { Spinner } from '@/components/ui/spinner';

import { RoleForm } from './role-form';
import { useRole } from './use-roles';

export function EditRole({ id }: { id: string }) {
  const { data: role, isLoading, isError } = useRole(id);

  if (isLoading) {
    return (
      <div className="text-muted-foreground flex items-center gap-2 text-sm" role="status">
        <Spinner className="size-4" />
        Loading role…
      </div>
    );
  }

  if (isError || !role) {
    return (
      <p className="text-muted-foreground rounded-md border border-dashed p-4 text-sm">
        That role could not be loaded. It may have been deleted.
      </p>
    );
  }

  return <RoleForm role={role} />;
}

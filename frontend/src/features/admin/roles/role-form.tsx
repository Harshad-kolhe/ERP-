'use client';

import { useRouter } from 'next/navigation';
import { Controller } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { FormError, TextField, TextareaField } from '@/components/form/fields';
import { useApiForm } from '@/components/form/use-api-form';
import { Button } from '@/components/ui/button';
import { Form } from '@/components/ui/form';
import { Spinner } from '@/components/ui/spinner';
import type { AdminRoleDetail } from '@/lib/api/types';

import { PermissionPicker } from './permission-picker';
import { usePermissionCatalogue, useSaveRole, type RoleFormValues } from './use-roles';

const schema = z.object({
  name: z.string().trim().min(1, 'Role name is required.').max(100, 'Name is too long.'),
  description: z.string().max(250, 'Description is too long.'),
  permissions: z.array(z.string()),
});

/**
 * Create and edit in one component. The two differ only in what they start from
 * and where they navigate afterwards; two files would drift on the first change.
 */
export function RoleForm({ role }: { role?: AdminRoleDetail }) {
  const router = useRouter();
  const catalogue = usePermissionCatalogue();
  const save = useSaveRole(role?.id);

  const { form, onSubmit, isSubmitting, formError } = useApiForm<RoleFormValues>({
    schema,
    defaultValues: {
      name: role?.name ?? '',
      description: role?.description ?? '',
      permissions: role?.permissions ?? [],
    },
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(role ? 'Role updated.' : 'Role created.');

      // A full reload rather than router.push: a user editing their own role needs
      // a fresh session before the change shows, because permissions are flattened
      // onto the principal at sign-in rather than read per request.
      router.push('/admin/roles');
      router.refresh();
    },
  });

  const granted = form.watch('permissions');

  return (
    <Form {...form}>
      <form onSubmit={onSubmit} noValidate className="max-w-3xl space-y-6">
        {/* Constrained rather than dropped into a two-column grid holding one
            field: the grid left an empty right half that read as a field somebody
            had forgotten to add. */}
        <TextField<RoleFormValues>
          name="name"
          label="Role name"
          required
          placeholder="Purchase Officer"
          disabled={isSubmitting}
          className="sm:max-w-sm"
        />

        <TextareaField<RoleFormValues>
          name="description"
          label="Description"
          description="What this role is for. Shown on the roles list."
          rows={2}
          disabled={isSubmitting}
        />

        <div className="space-y-2">
          <div className="flex items-baseline justify-between">
            <h2 className="text-sm font-semibold">Permissions</h2>
            {!role?.isSuperAdministrator ? (
              <span className="text-muted-foreground font-mono text-[11px]">
                {granted.length} granted
              </span>
            ) : null}
          </div>

          {role?.isSuperAdministrator ? (
            // No picker: this role grants from the catalogue, not from stored rows.
            // Showing checkboxes would invite an edit that silently does nothing.
            <p className="border-primary/30 bg-primary/5 rounded-md border px-3 py-2.5 text-[13px]">
              <strong className="font-semibold">This is a super-administrator role.</strong> It
              grants every permission the system defines — including permissions added by modules
              that ship in future — so there is nothing to choose here. To restrict what someone can
              do, give them an ordinary role instead of this one.
            </p>
          ) : (
            <>
              <p className="text-muted-foreground text-[13px]">
                What holders of this role may do. Users pick up changes at their next sign-in.
              </p>

              <Controller
                control={form.control}
                name="permissions"
                render={({ field }) => (
                  <PermissionPicker
                    catalogue={catalogue.data?.items ?? []}
                    isLoading={catalogue.isLoading}
                    isError={catalogue.isError}
                    value={field.value}
                    onChange={field.onChange}
                    disabled={isSubmitting}
                  />
                )}
              />
            </>
          )}
        </div>

        <FormError message={formError} />

        <div className="flex items-center gap-2 border-t pt-4">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <Spinner className="size-4" />}
            {role ? 'Save changes' : 'Create role'}
          </Button>
          <Button
            type="button"
            variant="ghost"
            disabled={isSubmitting}
            onClick={() => router.push('/admin/roles')}
          >
            Cancel
          </Button>

          {role && role.userCount > 0 ? (
            <p className="text-muted-foreground ml-auto text-[12.5px]">
              {role.userCount} user{role.userCount === 1 ? '' : 's'} hold this role
            </p>
          ) : null}
        </div>
      </form>
    </Form>
  );
}

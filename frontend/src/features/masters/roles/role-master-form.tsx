'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm, type MasterFormSection } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { RoleMasterDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { blankToNumber, numberToInput, useSaveMasterRecord } from '../shared/use-master-record';

export interface RoleMasterFormValues {
  roleId: string;
  rolesName: string;
  bypassBusinessUnit: boolean;
  isActive: boolean;
}

/** Mirrors `SaveRoleMasterValidator`. */
const schema = z.object({
  roleId: s.wholeNumber('Role id').refine((value) => value.trim() !== '', {
    message: 'Role id is required.',
  }),
  rolesName: s
    .requiredText(100, 'Role name')
    .regex(/^[0-9A-Za-z ]+$/, 'Role name may contain only letters, digits and spaces.'),
  bypassBusinessUnit: z.boolean(),
  isActive: z.boolean(),
}) satisfies z.ZodType<RoleMasterFormValues, RoleMasterFormValues>;

/**
 * The legacy role master form.
 *
 * Worth being blunt about what this is not: it grants nothing. Permissions live on
 * Identity roles and are edited under `/admin/roles`. These rows exist so an
 * employee's `Role id` has something to point at.
 *
 * The one exception is `Cross business unit`, which really does widen what a
 * holder can read — so it is on the form with the consequence spelled out, rather
 * than being a checkbox nobody could explain.
 */
export function RoleMasterForm({ role }: { role?: RoleMasterDetail }) {
  const router = useRouter();
  const isNew = !role;

  const save = useSaveMasterRecord<RoleMasterFormValues>({
    resource: 'roles',
    id: role?.id,
    rowVersion: role?.rowVersion,
    toBody: (values) => ({
      ...(isNew ? { roleId: blankToNumber(values.roleId) } : {}),
      rolesName: values.rolesName.trim(),
      bypassBusinessUnit: values.bypassBusinessUnit,
      isActive: values.isActive,
    }),
  });

  const sections = useMemo<MasterFormSection<RoleMasterFormValues>[]>(
    () => [
      {
        id: 'role',
        label: 'Role',
        description:
          'This is the legacy role master. It does not grant permissions — those are set on the roles administration screen.',
        fields: [
          {
            name: 'roleId',
            label: 'Role id',
            kind: 'integer',
            required: true,
            readOnly: !isNew,
            description: 'The number employee records reference.',
          },
          { name: 'rolesName', label: 'Roles name', required: true },
          {
            name: 'bypassBusinessUnit',
            label: 'Cross business unit',
            kind: 'boolean',
            wide: true,
            description: 'Lets holders read every business unit’s data, not just their own.',
          },
          { name: 'isActive', label: 'Active', kind: 'boolean', wide: true },
        ],
      },
    ],
    [isNew],
  );

  const { form, onSubmit, isSubmitting, formError } = useApiForm<RoleMasterFormValues>({
    schema,
    defaultValues: {
      roleId: numberToInput(role?.roleId),
      rolesName: role?.rolesName ?? '',
      bypassBusinessUnit: role?.bypassBusinessUnit ?? false,
      isActive: role?.isActive ?? true,
    },
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Role created.' : 'Role updated.');
      router.push('/masters/roles');
      router.refresh();
    },
  });

  return (
    <MasterForm<RoleMasterFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Create role' : 'Save changes'}
      onCancel={() => router.push('/masters/roles')}
    />
  );
}

'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { apiFetch } from '@/lib/api/fetcher';
import type { PagedResult, PermissionDefinition, AdminRoleDetail } from '@/lib/api/types';

/**
 * `all` is `['admin', 'roles']` — the same prefix `useMasterList` builds from a
 * `basePath` of `/admin`, so saving a role through `useSaveRole` still
 * invalidates the list the grid is reading. The list hook that used to live here
 * went with the bespoke table it fed.
 */
export const roleKeys = {
  all: ['admin', 'roles'] as const,
  detail: (id: string) => [...roleKeys.all, 'detail', id] as const,
  permissions: ['admin', 'permissions'] as const,
};

export function useRole(id: string) {
  return useQuery({
    queryKey: roleKeys.detail(id),
    queryFn: () => apiFetch<AdminRoleDetail>(`/admin/roles/${id}`),
  });
}

/**
 * The permission catalogue. Cached indefinitely: it is assembled from code at
 * startup and cannot change without a deployment, so re-fetching it per screen
 * would be pure noise.
 */
export function usePermissionCatalogue() {
  return useQuery({
    queryKey: roleKeys.permissions,
    queryFn: () => apiFetch<PagedResult<PermissionDefinition>>('/masters/permissions'),
    staleTime: Infinity,
  });
}

export interface RoleFormValues {
  name: string;
  description: string;
  permissions: string[];
}

export function useSaveRole(id?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    // Both branches typed void: create does return the new id, but the form
    // navigates to the list either way, and a union return type here buys nothing
    // except a generic-inference failure.
    mutationFn: async (values: RoleFormValues): Promise<void> => {
      if (id) {
        await apiFetch<void>(`/admin/roles/${id}`, { method: 'PUT', body: JSON.stringify(values) });
        return;
      }

      await apiFetch<{ id: string }>('/admin/roles', {
        method: 'POST',
        body: JSON.stringify(values),
      });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: roleKeys.all }),
  });
}

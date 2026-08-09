'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { apiFetch } from '@/lib/api/fetcher';
import type { PagedResult, PermissionDefinition, AdminRoleDetail, AdminRoleListItem } from '@/lib/api/types';

export const roleKeys = {
  all: ['admin', 'roles'] as const,
  list: (query: string) => [...roleKeys.all, 'list', query] as const,
  detail: (id: string) => [...roleKeys.all, 'detail', id] as const,
  permissions: ['admin', 'permissions'] as const,
};

export function useRoles(queryString: string) {
  return useQuery({
    queryKey: roleKeys.list(queryString),
    queryFn: () => apiFetch<PagedResult<AdminRoleListItem>>(`/admin/roles?${queryString}`),
    placeholderData: (previous) => previous,
  });
}

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

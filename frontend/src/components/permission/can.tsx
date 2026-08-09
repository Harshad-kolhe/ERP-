'use client';

import type { ReactNode } from 'react';

import { usePermissions } from './session-provider';

/**
 * Renders its children only when the user holds the permission.
 *
 * <Can permission="masters.part.create">
 *   <Button>New part</Button>
 * </Can>
 *
 * For deciding what to draw. The endpoint behind that button enforces the same
 * permission server-side, so this wrapper failing open would be untidy, not unsafe.
 */
export function Can({
  permission,
  children,
  fallback = null,
}: {
  permission: string;
  children: ReactNode;
  /** Shown instead when the permission is absent. Usually nothing. */
  fallback?: ReactNode;
}) {
  const { can } = usePermissions();

  return can(permission) ? <>{children}</> : <>{fallback}</>;
}

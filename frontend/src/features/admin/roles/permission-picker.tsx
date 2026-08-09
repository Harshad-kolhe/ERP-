'use client';

import { useMemo } from 'react';

import { Checkbox } from '@/components/ui/checkbox';
import { Spinner } from '@/components/ui/spinner';
import type { PermissionDefinition } from '@/lib/api/types';

/**
 * The control that makes the permission model configurable.
 *
 * Grouped by module and then by feature, because a flat list of every permission
 * in the system is unusable the moment there are more than about twenty — and the
 * legacy application had hundreds of screen/control rows with no grouping at all.
 *
 * Each group offers select-all, since granting "everything about parts" is the
 * common case and ticking five boxes to say it is friction with no purpose.
 */
export function PermissionPicker({
  catalogue,
  isLoading,
  value,
  onChange,
  disabled,
}: {
  catalogue: PermissionDefinition[];
  isLoading: boolean;
  value: string[];
  onChange: (next: string[]) => void;
  disabled?: boolean;
}) {
  const granted = useMemo(() => new Set(value), [value]);

  // module -> group -> permissions, preserving the catalogue's ordering.
  const grouped = useMemo(() => {
    const modules = new Map<string, Map<string, PermissionDefinition[]>>();

    for (const permission of catalogue) {
      const groups = modules.get(permission.module) ?? new Map<string, PermissionDefinition[]>();
      const bucket = groups.get(permission.group) ?? [];

      bucket.push(permission);
      groups.set(permission.group, bucket);
      modules.set(permission.module, groups);
    }

    return modules;
  }, [catalogue]);

  function toggle(code: string, checked: boolean) {
    onChange(checked ? [...value, code] : value.filter((granted) => granted !== code));
  }

  function toggleGroup(codes: string[], checked: boolean) {
    const others = value.filter((code) => !codes.includes(code));
    onChange(checked ? [...others, ...codes] : others);
  }

  if (isLoading) {
    return (
      <div className="text-muted-foreground flex items-center gap-2 rounded-md border p-4 text-sm">
        <Spinner className="size-4" />
        Loading permissions…
      </div>
    );
  }

  if (catalogue.length === 0) {
    return (
      <p className="text-muted-foreground rounded-md border border-dashed p-4 text-sm">
        No permissions are defined. That means no module published an IPermissionSource.
      </p>
    );
  }

  return (
    <div className="divide-y rounded-md border">
      {[...grouped].map(([module, groups]) => (
        <section key={module}>
          <h3 className="bg-muted/50 text-muted-foreground px-4 py-2 font-mono text-[10px] tracking-[0.1em] uppercase">
            {module}
          </h3>

          <div className="divide-y">
            {[...groups].map(([group, permissions]) => {
              const codes = permissions.map((permission) => permission.code);
              const allGranted = codes.every((code) => granted.has(code));
              const someGranted = !allGranted && codes.some((code) => granted.has(code));

              return (
                <div key={group} className="px-4 py-3">
                  <label className="flex cursor-pointer items-center gap-2.5">
                    <Checkbox
                      checked={allGranted ? true : someGranted ? 'indeterminate' : false}
                      onCheckedChange={(checked) => toggleGroup(codes, checked === true)}
                      disabled={disabled}
                    />
                    <span className="text-[13px] font-medium">{group}</span>
                    <span className="text-muted-foreground ml-auto font-mono text-[11px]">
                      {codes.filter((code) => granted.has(code)).length}/{codes.length}
                    </span>
                  </label>

                  <div className="mt-2.5 ml-6 grid gap-2 sm:grid-cols-2">
                    {permissions.map((permission) => (
                      <label
                        key={permission.code}
                        className="flex cursor-pointer items-start gap-2.5"
                        title={permission.code}
                      >
                        <Checkbox
                          checked={granted.has(permission.code)}
                          onCheckedChange={(checked) => toggle(permission.code, checked === true)}
                          disabled={disabled}
                          className="mt-0.5"
                        />
                        <span className="text-[13px] leading-tight">
                          {permission.name}
                          {/* The code shown alongside the label: it is what appears in an
                              audit log and in a 403 response, so people need to recognise it. */}
                          <span className="text-muted-foreground/70 block font-mono text-[10.5px]">
                            {permission.code}
                          </span>
                        </span>
                      </label>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      ))}
    </div>
  );
}

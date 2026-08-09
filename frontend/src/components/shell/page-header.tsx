import type { ReactNode } from 'react';

/**
 * The top of every screen: what this is, what it is for, and the primary actions.
 *
 * One component so all ~180 screens share a headline size, a description position
 * and an action alignment. The alternative is what the legacy application had —
 * every screen inventing its own header, and none of them agreeing.
 */
export function PageHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: string;
  /** Primary actions, right-aligned. Wrap each in <Can> where it needs a permission. */
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-3 border-b px-6 py-4">
      <div className="min-w-0">
        <h1 className="text-lg font-semibold tracking-tight">{title}</h1>
        {description ? (
          <p className="text-muted-foreground mt-0.5 max-w-2xl text-[13px]">{description}</p>
        ) : null}
      </div>
      {actions ? <div className="flex shrink-0 items-center gap-2">{actions}</div> : null}
    </div>
  );
}

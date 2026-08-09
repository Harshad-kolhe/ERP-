'use client';

import { useCallback, useEffect, useId, useState, type ReactNode } from 'react';

type PopoverProps = Readonly<{
  /** Rendered inside the trigger button. */
  label: ReactNode;
  title?: string;
  align?: 'left' | 'right';
  width?: number;
  children: (close: () => void) => ReactNode;
}>;

export default function Popover({ label, title, align = 'right', width = 260, children }: PopoverProps) {
  const [open, setOpen] = useState(false);
  const rootId = useId();
  const panelId = `${rootId}-panel`;
  const triggerId = `${rootId}-trigger`;

  /**
   * Focus returns to the trigger by id rather than through a ref, so this stays a
   * plain deferred callback that can be handed to the render prop. Without it,
   * closing from inside the panel unmounts the subtree holding focus and drops
   * the user at <body>.
   */
  const closeAndReturnFocus = useCallback(() => {
    setOpen(false);
    document.getElementById(triggerId)?.focus();
  }, [triggerId]);

  useEffect(() => {
    if (!open) return;

    const root = document.getElementById(rootId);

    const onPointerDown = (event: PointerEvent) => {
      if (root?.contains(event.target as Node)) return;
      // Only reclaim focus if it was inside the panel we are about to unmount;
      // otherwise we would steal it from whatever the user just clicked.
      if (root?.contains(document.activeElement)) closeAndReturnFocus();
      else setOpen(false);
    };
    const onKeyDown = (event: KeyboardEvent) => {
      // Non-modal: Tab must still move on. Only Escape closes and returns focus.
      if (event.key === 'Escape') closeAndReturnFocus();
    };

    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open, rootId, closeAndReturnFocus]);

  return (
    <div id={rootId} className="relative">
      <button
        id={triggerId}
        type="button"
        title={title}
        aria-expanded={open}
        aria-controls={open ? panelId : undefined}
        onClick={() => setOpen((value) => !value)}
        className={`border-border text-muted-foreground hover:border-line-strong hover:text-foreground focus-visible:ring-primary inline-flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-colors outline-none focus-visible:ring-2 ${
          open ? 'bg-accent text-foreground' : 'bg-card'
        }`}
      >
        {label}
      </button>

      {open && (
        /*
         * A plain container, not a `dialog`.
         *
         * It used to carry `role="dialog"` plus `aria-haspopup="dialog"`, which
         * promise assistive tech a modal boundary with managed focus — the exact
         * opposite of what this is, by design (see the Escape-only handler above:
         * Tab must keep moving). `aria-label={title}` was also undefined for the
         * column chooser, which passes no title, leaving an unnamed dialog.
         *
         * The trigger's `aria-expanded` and `aria-controls` already state the
         * whole relationship, and they are honest about it.
         */
        <div
          id={panelId}
          className="border-border bg-card animate-pop-in absolute top-9 z-50 rounded-xl border p-1.5 shadow-xl shadow-black/10"
          // Clamped so a right-aligned panel next to a right-edge trigger cannot
          // push the page sideways on a narrow viewport.
          style={{ width, maxWidth: 'calc(100vw - 1rem)', [align]: 0 }}
        >
          {children(closeAndReturnFocus)}
        </div>
      )}
    </div>
  );
}

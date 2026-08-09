'use client';

import { useTheme } from 'next-themes';
import { Monitor, Moon, Sun } from 'lucide-react';

import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

/**
 * Three choices, not a two-state switch: "match system" is a real preference, and a toggle that
 * flips light/dark silently discards it the first time someone touches it. The icon shows what is
 * actually being rendered, not what was chosen.
 */
export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Change theme">
          {/* Both icons render and CSS picks one. The usual mounted-flag dance exists because the
              server cannot know the system preference — but the `dark` class next-themes writes on
              <html> before hydration already answers that, so there is nothing to wait for. */}
          <Sun className="size-4 dark:hidden" />
          <Moon className="hidden size-4 dark:block" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {(
          [
            ['light', 'Light', Sun],
            ['dark', 'Dark', Moon],
            ['system', 'Match system', Monitor],
          ] as const
        ).map(([value, label, ItemIcon]) => (
          <DropdownMenuItem
            key={value}
            onClick={() => setTheme(value)}
            aria-current={theme === value}
            className={theme === value ? 'font-medium' : undefined}
          >
            <ItemIcon className="size-4" />
            {label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

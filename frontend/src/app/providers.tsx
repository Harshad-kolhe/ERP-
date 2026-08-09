'use client';

import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from 'next-themes';
import { useState, type ReactNode } from 'react';
import { toast } from 'sonner';

import { ApiError } from '@/lib/api/problem-details';

function describe(error: unknown) {
  return error instanceof Error ? error.message : 'Request failed.';
}

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Retrying a 4xx just repeats the same rejection. Because the API returns
          // real status codes this is a decision the client can actually make — with
          // the legacy `200 + Status:false` envelope it could not tell success from
          // failure at all.
          if (error instanceof ApiError && error.problem.status < 500) return false;
          return failureCount < 2;
        },
      },
    },
    // The floor for error feedback, decided once instead of per screen. A screen
    // that needs to say something specific still handles its own error.
    queryCache: new QueryCache({ onError: (error) => toast.error(describe(error)) }),
    mutationCache: new MutationCache({
      onError: (error) => {
        // Validation failures are already on screen, attached to the offending
        // field by useApiForm. A toast as well would be the same news twice, and
        // it would cover the field the user needs to look at.
        if (error instanceof ApiError && error.isValidation) return;

        toast.error(describe(error));
      },
    }),
  });
}

export function Providers({ children }: { children: ReactNode }) {
  // useState, not a module-level client: a module singleton is shared across
  // requests on the server and would leak one user's cached reads into another's render.
  const [queryClient] = useState(makeQueryClient);

  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </ThemeProvider>
  );
}

'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import Link from 'next/link';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
import { apiFetch } from '@/lib/api/fetcher';

const schema = z.object({
  email: z.email('Please enter a valid email address.'),
  password: z.string().min(1, 'Please enter your password.'),
});

type Credentials = z.infer<typeof schema>;

/**
 * The one sign-in form.
 *
 * The request goes to the BFF route handler on this origin, which forwards it to
 * the API and passes the resulting Set-Cookie back. No token is ever visible to
 * script — the session cookie is HttpOnly and same-origin.
 */
export function SignInForm({ returnUrl }: { returnUrl: string }) {
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Credentials>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
    // Checked on blur and then live — see the note in `useApiForm`, which sets the
    // same mode for every other form in the app.
    mode: 'onTouched',
  });

  const signIn = useMutation({
    mutationFn: (credentials: Credentials) =>
      apiFetch<void>('/auth/login', { method: 'POST', body: JSON.stringify(credentials) }),
    // Full navigation, not router.push. The cookie only exists after this response,
    // and the tab's router cache still holds the signed-out shell. isPending stays
    // true through this, so the button never re-enables mid-navigation.
    onSuccess: () => {
      window.location.href = returnUrl;
    },
    // No onError: the message is already on screen from the global mutation handler,
    // and letting the failure fall through is what leaves the form open and re-enabled.
  });

  return (
    <form
      onSubmit={handleSubmit((credentials) => signIn.mutate(credentials))}
      noValidate
      className="space-y-4"
    >
      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          type="email"
          autoComplete="username"
          spellCheck={false}
          aria-invalid={!!errors.email}
          aria-describedby={errors.email ? 'email-error' : undefined}
          {...register('email')}
        />
        {errors.email && (
          <p id="email-error" className="text-destructive text-sm">
            {errors.email.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label htmlFor="password">Password</Label>
          {/* aria-pressed and a real label: "Show" on its own announces a button
              with no object, and the visible word changes to "Hide" once pressed,
              which leaves a screen reader describing the action rather than the
              state. */}
          <button
            type="button"
            aria-pressed={showPassword}
            aria-controls="password"
            aria-label={showPassword ? 'Hide password' : 'Show password'}
            className="text-muted-foreground hover:text-foreground focus-visible:ring-ring rounded text-xs focus-visible:ring-2 focus-visible:outline-none"
            onClick={() => setShowPassword((shown) => !shown)}
          >
            {showPassword ? 'Hide' : 'Show'}
          </button>
        </div>
        <Input
          id="password"
          type={showPassword ? 'text' : 'password'}
          autoComplete="current-password"
          aria-invalid={!!errors.password}
          aria-describedby={errors.password ? 'password-error' : undefined}
          {...register('password')}
        />
        {errors.password && (
          <p id="password-error" className="text-destructive text-sm">
            {errors.password.message}
          </p>
        )}
      </div>

      <Link
        className="text-muted-foreground hover:text-foreground block text-sm underline-offset-4 hover:underline"
        href="/forgot-password"
      >
        Forgot your password?
      </Link>

      <Button type="submit" className="w-full" disabled={signIn.isPending}>
        {signIn.isPending && <Spinner className="size-4" />}
        {signIn.isPending ? 'Signing in' : 'Sign in'}
      </Button>

      <p className="text-muted-foreground text-center text-sm">
        New here?{' '}
        <Link className="text-primary font-medium underline-offset-4 hover:underline" href="/register">
          How to get an account
        </Link>
      </p>
    </form>
  );
}

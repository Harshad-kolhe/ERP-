'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import Link from 'next/link';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';

/**
 * Mirrors the password policy in the API's `AddErpAuthentication`, so a policy
 * failure shows before a round trip rather than as a toast afterwards. If the
 * server rules change, these change with them — the server stays the authority.
 */
const schema = z
  .object({
    fullName: z.string().trim().min(1, 'Please enter your name.').max(200, 'Name is too long.'),
    email: z.email('Please enter a valid email address.').max(256, 'Email is too long.'),
    password: z
      .string()
      .min(12, 'Use at least 12 characters.')
      .regex(/[A-Z]/, 'Include an uppercase letter.')
      .regex(/[a-z]/, 'Include a lowercase letter.')
      .regex(/[0-9]/, 'Include a digit.')
      .regex(/[^A-Za-z0-9]/, 'Include a symbol.'),
    confirmPassword: z.string(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'The two passwords do not match.',
    path: ['confirmPassword'],
  });

type Registration = z.infer<typeof schema>;

export function RegisterForm({
  returnUrl,
  onSwitchToSignIn,
}: {
  returnUrl: string;
  onSwitchToSignIn?: () => void;
}) {
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Registration>({
    resolver: zodResolver(schema),
    defaultValues: { fullName: '', email: '', password: '', confirmPassword: '' },
  });

  const createAccount = useMutation({
    // TODO(auth): wire to the API once the account-provisioning decision is made.
    // There is deliberately no POST /auth/register today — an ERP account grants
    // access to costing, suppliers and stock, so who may create one is a policy
    // question, not a default. The form is complete and validated; only the call
    // is missing, and it is one line when the endpoint exists.
    mutationFn: (_registration: Registration) =>
      Promise.reject(new Error('Account creation is not enabled yet. Ask an administrator to create your account.')),
    onSuccess: () => {
      window.location.href = returnUrl;
    },
    onError: (error: Error) => toast.error(error.message),
  });

  return (
    <form
      onSubmit={handleSubmit((registration) => createAccount.mutate(registration))}
      noValidate
      className="space-y-4"
    >
      <div className="space-y-2">
        <Label htmlFor="fullName">Full name</Label>
        <Input
          id="fullName"
          autoComplete="name"
          aria-invalid={!!errors.fullName}
          {...register('fullName')}
        />
        {errors.fullName && <p className="text-destructive text-sm">{errors.fullName.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="register-email">Email</Label>
        <Input
          id="register-email"
          type="email"
          autoComplete="username"
          spellCheck={false}
          aria-invalid={!!errors.email}
          {...register('email')}
        />
        {errors.email && <p className="text-destructive text-sm">{errors.email.message}</p>}
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label htmlFor="register-password">Password</Label>
          <button
            type="button"
            className="text-muted-foreground hover:text-foreground text-xs"
            onClick={() => setShowPassword((shown) => !shown)}
          >
            {showPassword ? 'Hide' : 'Show'}
          </button>
        </div>
        <Input
          id="register-password"
          type={showPassword ? 'text' : 'password'}
          autoComplete="new-password"
          aria-invalid={!!errors.password}
          aria-describedby="password-rules"
          {...register('password')}
        />
        {/* The rules stated plainly, rather than a strength meter. A meter scores a
            password without ever saying what would satisfy the server, which is the
            only thing the user needs. */}
        <p id="password-rules" className="text-muted-foreground text-xs">
          At least 12 characters, with an uppercase letter, a lowercase letter, a digit and a
          symbol.
        </p>
        {errors.password && <p className="text-destructive text-sm">{errors.password.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="confirmPassword">Confirm password</Label>
        <Input
          id="confirmPassword"
          type={showPassword ? 'text' : 'password'}
          autoComplete="new-password"
          aria-invalid={!!errors.confirmPassword}
          {...register('confirmPassword')}
        />
        {errors.confirmPassword && (
          <p className="text-destructive text-sm">{errors.confirmPassword.message}</p>
        )}
      </div>

      {/* Said up front rather than discovered later: a new account holds no
          permissions and no business unit, so it sees empty screens until an
          administrator grants them. */}
      <p className="bg-muted text-muted-foreground rounded-md p-3 text-xs">
        New accounts start with <strong>no permissions</strong> and no business unit. An
        administrator has to grant those before any screen will show data.
      </p>

      <Button type="submit" className="w-full" disabled={createAccount.isPending}>
        {createAccount.isPending && <Spinner className="size-4" />}
        {createAccount.isPending ? 'Creating account' : 'Create account'}
      </Button>

      <p className="text-muted-foreground text-center text-sm">
        Already have an account?{' '}
        {onSwitchToSignIn ? (
          <button
            type="button"
            onClick={onSwitchToSignIn}
            className="text-primary font-medium underline-offset-4 hover:underline"
          >
            Sign in
          </button>
        ) : (
          <Link
            className="text-primary font-medium underline-offset-4 hover:underline"
            href={{ pathname: '/login', query: { returnUrl } }}
          >
            Sign in
          </Link>
        )}
      </p>
    </form>
  );
}

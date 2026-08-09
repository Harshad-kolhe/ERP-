import { AuthFrame } from '@/components/auth/auth-frame';
import { SignInForm } from '@/components/auth/sign-in-form';
import { safeReturnUrl } from '@/lib/routes';

export const metadata = { title: 'Sign in · ERP' };

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ returnUrl?: string | string[] }>;
}) {
  const { returnUrl } = await searchParams;

  return (
    <AuthFrame heading="Welcome back" subheading="Sign in to continue.">
      <SignInForm returnUrl={safeReturnUrl(returnUrl)} />
    </AuthFrame>
  );
}

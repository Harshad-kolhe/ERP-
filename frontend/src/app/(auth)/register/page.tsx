import { AuthFrame } from '@/components/auth/auth-frame';
import { RegisterForm } from '@/components/auth/register-form';
import { safeReturnUrl } from '@/lib/routes';

export const metadata = { title: 'Create account · ERP' };

export default async function RegisterPage({
  searchParams,
}: {
  searchParams: Promise<{ returnUrl?: string | string[] }>;
}) {
  const { returnUrl } = await searchParams;

  return (
    <AuthFrame
      heading="Create an account"
      subheading="Set up your sign-in details to get started."
    >
      <RegisterForm returnUrl={safeReturnUrl(returnUrl)} />
    </AuthFrame>
  );
}

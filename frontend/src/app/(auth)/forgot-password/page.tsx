import Link from 'next/link';

import { AuthFrame } from '@/components/auth/auth-frame';

export const metadata = { title: 'Forgot password · ERP' };

/**
 * A placeholder that says so, rather than a form that silently does nothing.
 *
 * Password reset needs an email sender and a token flow on the API, neither of
 * which exists yet. A form here would collect an address and quietly discard it,
 * and the user would sit waiting for a mail that is never coming.
 */
export default function ForgotPasswordPage() {
  return (
    <AuthFrame
      heading="Forgot your password?"
      subheading="Self-service reset is not available yet."
      footer={
        <Link className="text-primary font-medium underline-offset-4 hover:underline" href="/login">
          Back to sign in
        </Link>
      }
    >
      <p className="bg-muted text-muted-foreground rounded-md p-3 text-sm">
        Ask an administrator to reset your password. Self-service reset arrives with the email
        sender.
      </p>
    </AuthFrame>
  );
}

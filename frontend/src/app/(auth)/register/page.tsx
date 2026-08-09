import Link from 'next/link';

import { AuthFrame } from '@/components/auth/auth-frame';

export const metadata = { title: 'Create account · ERP' };

/**
 * Says accounts are not self-service, instead of a form that rejects on submit.
 *
 * This page used to render four validated fields, a password-rules hint and a
 * real-looking button over a `mutationFn` that was a hardcoded rejection: the
 * user filled it all in and *then* got a toast saying the feature did not exist.
 *
 * Every other unbuilt thing in this app is honest before the effort, not after —
 * `/forgot-password` says so and renders no form, planned nav items are inert with
 * a "soon" badge, unavailable menu actions carry a reason. This now matches them.
 *
 * TODO(auth): there is deliberately no `POST /auth/register`. An ERP account grants
 * access to costing, suppliers and stock, so who may create one is a policy
 * question rather than a default. When that is decided, rebuild the form against
 * `lib/auth/password-policy.ts`, which is the validated schema this page dropped.
 */
export default function RegisterPage() {
  return (
    <AuthFrame
      heading="Create an account"
      subheading="Accounts are created by an administrator."
      footer={
        <Link className="text-primary font-medium underline-offset-4 hover:underline" href="/login">
          Back to sign in
        </Link>
      }
    >
      <p className="bg-muted text-muted-foreground rounded-md p-3 text-sm">
        An account here grants access to costing, suppliers and stock, so accounts are set up by an
        administrator rather than by signing up. Ask yours to create one for you — they will also
        assign the modules you need.
      </p>
    </AuthFrame>
  );
}

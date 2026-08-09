import { ArrowRight, FileText } from 'lucide-react';
import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { PartStatusCard } from '@/features/dashboard/part-status-card';
import { QueueStrip } from '@/features/dashboard/queue-strip';
import { getSession } from '@/lib/auth/session';

export const metadata = { title: 'Dashboard · ERP' };

/**
 * The dashboard.
 *
 * A first cut, built only from data that exists. The full design — machines in
 * build, shortages blocking assembly, the procure-to-receive pipeline with stage
 * ageing — is in docs/design/erp-interface-design.html and lands as the modules
 * feeding it do.
 *
 * Blocks whose module does not exist are stated as pending rather than shown as
 * zero. "0 purchase orders overdue" claims there are none; the truth is that
 * purchasing has not been built, and a buyer reading the first would be misled.
 *
 * It lives at `/home` rather than `/` because `/` is the public project overview,
 * which has to render for someone who has not signed in yet.
 */
export default async function HomePage() {
  const user = await getSession();

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title={user?.userName ? `Welcome back, ${user.userName}` : 'Dashboard'}
        description="What needs you today. Every number opens the list behind it."
      />

      <div className="min-h-0 flex-1 space-y-5 overflow-y-auto p-6">
        <Can
          permission="masters.part.read"
          fallback={
            <p className="text-muted-foreground rounded-md border border-dashed p-4 text-sm">
              Nothing to show yet. Ask an administrator to grant you access to a module.
            </p>
          }
        >
          <Suspense fallback={null}>
            <QueueStrip />
          </Suspense>

          {/* Two halves of real content. "Still to come" used to sit here and take
              half the fold, which is what made a working dashboard read as an empty
              one — a four-item roadmap is not a tile, and it has moved below. */}
          <div className="grid gap-4 lg:grid-cols-2">
            <Suspense fallback={null}>
              <PartStatusCard />
            </Suspense>

            <Link
              href="/"
              className="bg-card hover:border-primary/50 group flex items-start gap-3 rounded-md border p-4 transition-colors"
            >
              <FileText className="text-primary mt-0.5 size-4 shrink-0" aria-hidden />
              <span>
                <span className="flex items-center gap-1.5 text-sm font-semibold">
                  What this system does
                  <ArrowRight
                    className="size-3.5 opacity-0 transition-opacity group-hover:opacity-100"
                    aria-hidden
                  />
                </span>
                <span className="text-muted-foreground mt-0.5 block text-[13px] leading-relaxed">
                  How an order moves from enquiry to dispatch, what each department can do, and
                  which parts are ready to use today. Worth sending to anyone new.
                </span>
              </span>
            </Link>
          </div>
        </Can>

        <section className="bg-muted/30 rounded-md border border-dashed p-5">
          <h2 className="text-muted-foreground font-mono text-[10.5px] font-semibold tracking-[0.09em] uppercase">
            Still to come
          </h2>
          <p className="text-muted-foreground mt-2 max-w-3xl text-[13px] leading-relaxed">
            Machines in build, shortages blocking assembly, and the procure-to-receive pipeline. Each
            needs the module that feeds it — building them against invented data would mean building
            them twice.
          </p>
          <ol className="text-muted-foreground/80 mt-3 space-y-1 font-mono text-[11.5px]">
            <li>1 · Users &amp; role assignment</li>
            <li>2 · Part create, edit, approve</li>
            <li>3 · Suppliers, customers, employees</li>
            <li>4 · Procurement → receipts → stock</li>
          </ol>
        </section>
      </div>
    </div>
  );
}

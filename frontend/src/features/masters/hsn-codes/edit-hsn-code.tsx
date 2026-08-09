'use client';

import type { HsnCodeDetail } from '@/lib/api/types';
import { EditMasterRecord } from '../shared/edit-master-record';
import { HsnCodeForm } from './hsn-code-form';
import { HsnRateHistory } from './hsn-rate-history';

/**
 * Loads one HSN code, then renders the form and its rate history.
 *
 * The two are siblings rather than one form because they are two different
 * operations: the form replaces the code's description, and the history appends a
 * rate. See `EditRoleMaster` for why this component is the client boundary.
 */
export function EditHsnCode({ id }: { id: string }) {
  return (
    <EditMasterRecord<HsnCodeDetail> resource="hsn-codes" id={id} noun="HSN code">
      {(record) => (
        <div className="flex min-h-0 flex-col gap-4">
          <HsnCodeForm hsn={record} />
          <div className="px-4 pb-4 sm:px-6">
            <HsnRateHistory id={record.id} rates={record.rates} />
          </div>
        </div>
      )}
    </EditMasterRecord>
  );
}

'use client';

import type { LookupValueDetail } from '@/lib/api/types';
import { EditMasterRecord } from '../shared/edit-master-record';
import { LookupValueForm } from './lookup-value-form';

/** Loads one option, then hands it to the form. See `EditRoleMaster` for why this boundary exists. */
export function EditLookupValue({ id }: { id: string }) {
  return (
    <EditMasterRecord<LookupValueDetail> resource="lookup-values" id={id} noun="option">
      {(record) => <LookupValueForm value={record} />}
    </EditMasterRecord>
  );
}

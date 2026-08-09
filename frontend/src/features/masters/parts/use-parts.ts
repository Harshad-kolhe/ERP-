'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '@/lib/api/fetcher';
import type { PagedResult, PartDetail, PartListItem } from '@/lib/api/types';
import type { PartFormValues } from './part-form-fields';

export const partsQueryKeys = {
  all: ['masters', 'parts'] as const,
  list: (queryString: string) => [...partsQueryKeys.all, 'list', queryString] as const,
  detail: (id: string) => [...partsQueryKeys.all, 'detail', id] as const,
};

/**
 * Fetches one page of parts.
 *
 * The query key includes the full query string, so every distinct combination of
 * page, sort, filter and search is cached separately and going back to a previous
 * page is instant without re-fetching.
 */
export function usePartsList(queryString: string) {
  return useQuery({
    queryKey: partsQueryKeys.list(queryString),
    queryFn: () => apiFetch<PagedResult<PartListItem>>(`/masters/parts?${queryString}`),
    placeholderData: (previous) => previous,
  });
}

/** One part, for the edit screen. */
export function usePart(id: string) {
  return useQuery({
    queryKey: partsQueryKeys.detail(id),
    queryFn: () => apiFetch<PartDetail>(`/masters/parts/${id}`),
  });
}

/**
 * Creates or updates a part.
 *
 * The two are one hook because the form is one form; the only differences are the
 * verb, the part number (absent from an update — it is the business key) and the
 * row version, which must go back exactly as it came so a concurrent edit yields
 * 409 rather than silently winning.
 */
export function useSavePart(part?: PartDetail) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (values: PartFormValues): Promise<void> => {
      const body = {
        description: values.description.trim(),
        categoryId: null,
        unitOfMeasureCode: values.unitOfMeasureCode.trim(),
        hsnCode: blankToNull(values.hsnCode),
        drawingNumber: blankToNull(values.drawingNumber),
        attributes: {
          itemNumber: blankToNull(values.itemNumber),
          technicalSpecification: blankToNull(values.technicalSpecification),
          moc: blankToNull(values.moc),
          partCategoryCode: blankToNull(values.partCategoryCode),
          partType: blankToNull(values.partType),
          formCategory: blankToNull(values.formCategory),
          purchaseUomCode: blankToNull(values.purchaseUomCode),
          sellingUomCode: blankToNull(values.sellingUomCode),
          materialType: blankToNull(values.materialType),
          seriesCode: blankToNull(values.seriesCode),
          partRevisionNo: blankToNull(values.partRevisionNo),
          sourceCode: blankToNull(values.sourceCode),
          weightKg: blankToNumber(values.weightKg),
          leadTimeDays: blankToNumber(values.leadTimeDays),
          minimumStockLevel: blankToNumber(values.minimumStockLevel),
          reorderPoint: blankToNumber(values.reorderPoint),
          /*
           * Echoed back untouched, not edited here.
           *
           * These three are the reason a part was revised, held or withdrawn —
           * they belong to a status change, not to this form, so the form no
           * longer shows them. But the update endpoint takes `attributes` whole
           * and clears anything omitted, which is what makes a field deletable at
           * all. Leaving them out would therefore not "not edit" them: it would
           * wipe the reason every time somebody corrected a description.
           *
           * They move to the status action once that exists, and disappear from
           * here entirely.
           */
          revisionRemark: part?.attributes.revisionRemark ?? null,
          holdRemark: part?.attributes.holdRemark ?? null,
          inactiveRemark: part?.attributes.inactiveRemark ?? null,
        },
      };

      if (part) {
        await apiFetch<void>(`/masters/parts/${part.id}`, {
          method: 'PUT',
          body: JSON.stringify({ ...body, rowVersion: part.rowVersion }),
        });
        return;
      }

      await apiFetch<{ id: string }>('/masters/parts', {
        method: 'POST',
        body: JSON.stringify({ ...body, partNumber: values.partNumber.trim() }),
      });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: partsQueryKeys.all }),
  });
}

/**
 * An untouched text box holds "", and "" is not the same statement as null: the
 * first says "I typed nothing", the second says "this part has no HSN code". The
 * API models absence as null, so the mapping happens here rather than sending
 * empty strings the server would have to guess about.
 */
function blankToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

function blankToNumber(value: string): number | null {
  const trimmed = value.trim();
  if (trimmed === '') return null;

  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

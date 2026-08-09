using Erp.Contracts.Common;

namespace Erp.Modules.Masters.Application.Parts.ListParts;

/// <param name="Page">
/// Paging, sorting and filtering as the client asked for it. Normalised by the
/// handler, so an unbounded page size is impossible regardless of what arrives.
/// </param>
internal sealed record ListPartsQuery(PageRequest Page);

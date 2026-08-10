using Erp.Contracts.Common;

namespace Erp.Api.Features.BusinessUnits.ListBusinessUnits;

/// <param name="Page">Paging, sorting and filtering as the client asked for it. Normalised by the handler.</param>
public sealed record ListBusinessUnitsQuery(PageRequest Page);

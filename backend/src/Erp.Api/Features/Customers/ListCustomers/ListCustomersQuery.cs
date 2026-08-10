using Erp.Contracts.Common;

namespace Erp.Api.Features.Customers.ListCustomers;

/// <param name="Page">Paging, sorting and filtering as the client asked for it. Normalised by the handler.</param>
public sealed record ListCustomersQuery(PageRequest Page);

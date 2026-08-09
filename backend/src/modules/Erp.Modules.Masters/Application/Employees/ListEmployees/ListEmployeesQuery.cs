using Erp.Contracts.Common;

namespace Erp.Modules.Masters.Application.Employees.ListEmployees;

/// <param name="Page">Paging, sorting and filtering as the client asked for it. Normalised by the handler.</param>
internal sealed record ListEmployeesQuery(PageRequest Page);

using Erp.Contracts.Common;

namespace Erp.Modules.Masters.Application.Roles.ListRoles;

/// <param name="Page">Paging, sorting and filtering as the client asked for it. Normalised by the handler.</param>
internal sealed record ListRolesQuery(PageRequest Page);

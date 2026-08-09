using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Assemblies;

/// <summary>
/// Assembly Master — the middle level. Every assembly belongs to exactly one
/// section; the server enforces that, rather than trusting the picker.
/// </summary>
internal sealed class AssemblyEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group) =>
        new AssemblyNodeRoutes(
            Resource: "assemblies",
            Singular: "Assembly",
            Plural: "Assemblies",
            Label: "assembly",
            Level: AssemblyLevel.Assembly,
            ReadPermission: MastersPermissions.AssemblyRead,
            CreatePermission: MastersPermissions.AssemblyCreate,
            UpdatePermission: MastersPermissions.AssemblyUpdate)
            .Map(group);
}

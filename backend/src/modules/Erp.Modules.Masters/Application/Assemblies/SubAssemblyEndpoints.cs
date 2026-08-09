using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Domain.Assemblies;
using Erp.Modules.Masters.Integration;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Assemblies;

/// <summary>
/// Sub-assembly Master — the bottom level of the breakdown.
/// <para>
/// Its parent must be an assembly. The legacy screen also accepted a section, with
/// a further rule that refused the section route if any of that section's
/// assemblies already had sub-assemblies — a condition whose result depended on the
/// order records were entered in. One rule, applied the same way every time,
/// replaces it.
/// </para>
/// </summary>
internal sealed class SubAssemblyEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group) =>
        new AssemblyNodeRoutes(
            Resource: "sub-assemblies",
            Singular: "SubAssembly",
            Plural: "SubAssemblies",
            Label: "sub-assembly",
            Level: AssemblyLevel.SubAssembly,
            ReadPermission: MastersPermissions.SubAssemblyRead,
            CreatePermission: MastersPermissions.SubAssemblyCreate,
            UpdatePermission: MastersPermissions.SubAssemblyUpdate)
            .Map(group);
}

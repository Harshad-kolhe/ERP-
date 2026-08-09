using Erp.BuildingBlocks.Web.Modules;
using Erp.Modules.Masters.Integration;
using Erp.Persistence.Domain.Assemblies;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Assemblies;

/// <summary>
/// Section Master — the top of the machine breakdown.
/// <para>
/// The permissions are stated here rather than inside <see cref="AssemblyNodeRoutes"/>
/// so that "who may create a section" is answerable by reading this file, which is
/// the file named after the screen.
/// </para>
/// </summary>
internal sealed class SectionEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group) =>
        new AssemblyNodeRoutes(
            Resource: "sections",
            Singular: "Section",
            Plural: "Sections",
            Label: "section",
            Level: AssemblyLevel.Section,
            ReadPermission: MastersPermissions.SectionRead,
            CreatePermission: MastersPermissions.SectionCreate,
            UpdatePermission: MastersPermissions.SectionUpdate)
            .Map(group);
}

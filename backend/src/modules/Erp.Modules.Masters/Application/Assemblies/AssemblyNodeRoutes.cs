using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.Assemblies.ListAssemblyNodes;
using Erp.Modules.Masters.Application.Assemblies.WriteAssemblyNode;
using Erp.Modules.Masters.Domain.Assemblies;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.Assemblies;

/// <summary>
/// The route shape the three assembly-node masters share.
/// <para>
/// Sections, assemblies and sub-assemblies are one record type at three depths, so
/// their endpoints differ in exactly three things: the path segment, the level they
/// pin, and the permissions they require. Everything else — the paging contract,
/// the concurrency contract, the status codes, the OpenAPI text — is identical, and
/// writing it out three times is how three grids end up describing themselves three
/// different ways in the generated client.
/// </para>
/// <para>
/// Each master still gets its own <c>IEndpoint</c> class, so the endpoint table is
/// discovered per master and the permissions are stated at the call site rather
/// than buried in a loop.
/// </para>
/// </summary>
/// <param name="Resource">Path segment under <c>/masters</c>, e.g. <c>sections</c>.</param>
/// <param name="Singular">Used in endpoint names, e.g. <c>Section</c> → <c>GetSectionById</c>.</param>
/// <param name="Plural">
/// The list endpoint's name, spelled out rather than <c>Singular + "s"</c>: that
/// would produce <c>ListAssemblys</c>, and endpoint names become the operation ids
/// in OpenAPI and therefore the method names in the generated TypeScript client.
/// </param>
/// <param name="Label">Human-readable, lower case, for prose: "section", "sub-assembly".</param>
internal sealed record AssemblyNodeRoutes(
    string Resource,
    string Singular,
    string Plural,
    string Label,
    AssemblyLevel Level,
    string ReadPermission,
    string CreatePermission,
    string UpdatePermission)
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        MapList(group);
        MapGetById(group);
        MapCreate(group);
        MapUpdate(group);
    }

    private void MapList(RouteGroupBuilder group)
    {
        var level = Level;

        group.MapGet($"/{Resource}", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListAssemblyNodesQuery, PagedResult<AssemblyNodeListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListAssemblyNodesQuery(level, request), cancellationToken);

                return result.ToHttpResult();
            })
            .WithName($"List{Plural}")
            .WithSummary($"List {Label}s")
            .WithDescription(
                "Server-paged. Supports sort=field:asc|desc (comma-separated), "
                + "filter=field:op:value (semicolon-separated), and free-text search across "
                + "code, name, manual code and parent code. pageSize is clamped to 200.")
            .RequirePermission(ReadPermission)
            .Produces<PagedResult<AssemblyNodeListItemDto>>();
    }

    private void MapGetById(RouteGroupBuilder group)
    {
        var level = Level;

        group.MapGet($"/{Resource}/{{id:guid}}", async (
                Guid id,
                IQueryHandler<GetAssemblyNodeByIdQuery, AssemblyNodeDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetAssemblyNodeByIdQuery(level, id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName($"Get{Singular}ById")
            .WithSummary($"Get one {Label}")
            .WithDescription(
                "Returns every editable field, the parent's code and name for the picker, "
                + "and the rowVersion the update endpoint requires.")
            .RequirePermission(ReadPermission)
            .Produces<AssemblyNodeDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private void MapCreate(RouteGroupBuilder group)
    {
        var level = Level;
        var resource = Resource;

        group.MapPost($"/{Resource}", async (
                CreateAssemblyNodeRequest request,
                ICommandHandler<CreateAssemblyNodeCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new CreateAssemblyNodeCommand(level, request),
                    cancellationToken);

                return result.ToHttpResult(id =>
                    Results.Created($"/api/v1/masters/{resource}/{id}", new { id }));
            })
            .WithName($"Create{Singular}")
            .WithSummary($"Create a {Label}")
            .WithDescription(
                "The level comes from this route, not from the payload. The code is supplied by "
                + "the caller and must be unique across sections, assemblies and sub-assemblies.")
            .RequirePermission(CreatePermission)
            .WithValidation<CreateAssemblyNodeRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private void MapUpdate(RouteGroupBuilder group)
    {
        var level = Level;

        group.MapPut($"/{Resource}/{{id:guid}}", async (
                Guid id,
                UpdateAssemblyNodeRequest request,
                ICommandHandler<UpdateAssemblyNodeCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new UpdateAssemblyNodeCommand(level, id, request),
                    cancellationToken);

                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName($"Update{Singular}")
            .WithSummary($"Update a {Label}")
            .WithDescription(
                "Requires the rowVersion returned by GET. A stale value yields 409 rather than "
                + "overwriting a concurrent edit. The code and the level cannot be changed here.")
            .RequirePermission(UpdatePermission)
            .WithValidation<UpdateAssemblyNodeRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

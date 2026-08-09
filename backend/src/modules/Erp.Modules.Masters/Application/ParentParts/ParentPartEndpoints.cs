using Erp.BuildingBlocks.Application.Cqrs;
using Erp.BuildingBlocks.Web.Http;
using Erp.BuildingBlocks.Web.Modules;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Erp.Modules.Masters.Application.ParentParts.ListParentParts;
using Erp.Modules.Masters.Application.ParentParts.WriteParentPart;
using Erp.Modules.Masters.Integration;
using Erp.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Modules.Masters.Application.ParentParts;

/// <summary>
/// Parent Part Master — list, read-one, create and update.
/// <para>
/// There is no endpoint per component line, deliberately. A build is edited as a
/// whole and saved as a whole, so the lines travel with their header and the
/// totals are recomputed once inside one transaction. Line-level endpoints are what
/// let the legacy screen add a child and update the header's totals as two separate
/// writes that could disagree.
/// </para>
/// </summary>
internal sealed class ParentPartEndpoints : IEndpoint
{
    public void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/parent-parts", async (
                int? page,
                int? pageSize,
                string? sort,
                string? search,
                string? filter,
                IQueryHandler<ListParentPartsQuery, PagedResult<ParentPartListItemDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
                var result = await handler.HandleAsync(new ListParentPartsQuery(request), cancellationToken);

                return result.ToHttpResult();
            })
            .WithName("ListParentParts")
            .WithSummary("List parent parts")
            .WithDescription(
                "Server-paged. Supports sort=field:asc|desc (comma-separated), "
                + "filter=field:op:value (semicolon-separated), and free-text search across "
                + "part number, part description, build description and assembly code. "
                + "pageSize is clamped to 200.")
            .RequirePermission(MastersPermissions.ParentPartRead)
            .Produces<PagedResult<ParentPartListItemDto>>();

        group.MapGet("/parent-parts/{id:guid}", async (
                Guid id,
                IQueryHandler<GetParentPartByIdQuery, ParentPartDetailDto> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetParentPartByIdQuery(id), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetParentPartById")
            .WithSummary("Get one parent part")
            .WithDescription(
                "Returns the header, every component line in order with its part number resolved, "
                + "and the rowVersion the update endpoint requires.")
            .RequirePermission(MastersPermissions.ParentPartRead)
            .Produces<ParentPartDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/parent-parts", async (
                CreateParentPartRequest request,
                ICommandHandler<CreateParentPartCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CreateParentPartCommand(request), cancellationToken);
                return result.ToHttpResult(id => Results.Created($"/api/v1/masters/parent-parts/{id}", new { id }));
            })
            .WithName("CreateParentPart")
            .WithSummary("Create a parent part")
            .WithDescription(
                "Creates the build and its component lines in one transaction. A part may have "
                + "only one build; a second attempt yields 409. Weight and amount totals are "
                + "computed from the lines and are not read from the payload.")
            .RequirePermission(MastersPermissions.ParentPartCreate)
            .WithValidation<CreateParentPartRequest>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/parent-parts/{id:guid}", async (
                Guid id,
                UpdateParentPartRequest request,
                ICommandHandler<UpdateParentPartCommand, Unit> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new UpdateParentPartCommand(id, request), cancellationToken);
                return result.ToHttpResult(_ => Results.NoContent());
            })
            .WithName("UpdateParentPart")
            .WithSummary("Update a parent part")
            .WithDescription(
                "Replaces the header and the whole component list. Requires the rowVersion "
                + "returned by GET; a stale value yields 409 rather than overwriting a concurrent "
                + "edit, including its lines. The part being built cannot be changed here.")
            .RequirePermission(MastersPermissions.ParentPartUpdate)
            .WithValidation<UpdateParentPartRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

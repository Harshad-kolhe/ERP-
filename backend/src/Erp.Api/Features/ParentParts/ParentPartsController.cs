using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.ParentParts;

[ApiController]
[Route("api/v1/masters/parent-parts")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class ParentPartsController(
    ParentPartsService parentParts,
    ParentPartsQueries queries) : ControllerBase
{
    [HttpGet(Name = "ListParentParts")]
    [RequirePermission(MastersPermissions.ParentPartRead)]
    [EndpointSummary("List parent parts")]
    [EndpointDescription(
        "Server-paged. Supports sort=field:asc|desc (comma-separated), "
        + "filter=field:op:value (semicolon-separated), and free-text search across "
        + "part number, part description, build description and assembly code. "
        + "pageSize is clamped to 200.")]
    [ProducesResponseType<PagedResult<ParentPartListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? search,
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
    {
        var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
        var result = await queries.ListAsync(request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpGet("{id:guid}", Name = "GetParentPartById")]
    [RequirePermission(MastersPermissions.ParentPartRead)]
    [EndpointSummary("Get one parent part")]
    [EndpointDescription(
        "Returns the header, every component line in order with its part number resolved, "
        + "and the rowVersion the update endpoint requires.")]
    [ProducesResponseType<ParentPartDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateParentPart")]
    [RequirePermission(MastersPermissions.ParentPartCreate)]
    [EndpointSummary("Create a parent part")]
    [EndpointDescription(
        "Creates the build and its component lines in one transaction. A part may have "
        + "only one build; a second attempt yields 409. Weight and amount totals are "
        + "computed from the lines and are not read from the payload.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateParentPartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await parentParts.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/parent-parts/{id}", new { id }));
    }

    [HttpPut("{id:guid}", Name = "UpdateParentPart")]
    [RequirePermission(MastersPermissions.ParentPartUpdate)]
    [EndpointSummary("Update a parent part")]
    [EndpointDescription(
        "Replaces the header and the whole component list. Requires the rowVersion "
        + "returned by GET; a stale value yields 409 rather than overwriting a concurrent "
        + "edit, including its lines. The part being built cannot be changed here.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateParentPartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await parentParts.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }
}

using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Api.Domain.Assemblies;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.Assemblies;

[ApiController]
[Route("api/v1/masters/sub-assemblies")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class SubAssembliesController(
    AssemblyNodesService nodes,
    AssemblyNodesQueries queries) : ControllerBase
{
    private const AssemblyLevel Level = AssemblyLevel.SubAssembly;

    [HttpGet(Name = "ListSubAssemblies")]
    [RequirePermission(MastersPermissions.SubAssemblyRead)]
    [EndpointSummary("List sub-assemblys")]
    [EndpointDescription(
        "Server-paged. Supports sort=field:asc|desc (comma-separated), "
        + "filter=field:op:value (semicolon-separated), and free-text search across "
        + "code, name, manual code and parent code. pageSize is clamped to 200.")]
    [ProducesResponseType<PagedResult<AssemblyNodeListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? search,
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
    {
        var request = PageRequestBinding.From(page, pageSize, sort, search, filter);
        var result = await queries.ListAsync(Level, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpGet("{id:guid}", Name = "GetSubAssemblyById")]
    [RequirePermission(MastersPermissions.SubAssemblyRead)]
    [EndpointSummary("Get one sub-assembly")]
    [EndpointDescription(
        "Returns every editable field, the parent's code and name for the picker, "
        + "and the rowVersion the update endpoint requires.")]
    [ProducesResponseType<AssemblyNodeDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(Level, id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateSubAssembly")]
    [RequirePermission(MastersPermissions.SubAssemblyCreate)]
    [EndpointSummary("Create a sub-assembly")]
    [EndpointDescription(
        "The level comes from this route, not from the payload. The code is supplied by "
        + "the caller and must be unique across sections, assemblies and sub-assemblies.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateAssemblyNodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await nodes.CreateAsync(Level, request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/sub-assemblies/{id}", new { id }));
    }

    [HttpPut("{id:guid}", Name = "UpdateSubAssembly")]
    [RequirePermission(MastersPermissions.SubAssemblyUpdate)]
    [EndpointSummary("Update a sub-assembly")]
    [EndpointDescription(
        "Requires the rowVersion returned by GET. A stale value yields 409 rather than "
        + "overwriting a concurrent edit. The code and the level cannot be changed here.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateAssemblyNodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await nodes.UpdateAsync(Level, id, request, cancellationToken);

        return result.ToHttpResult();
    }
}

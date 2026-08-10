using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.UnitsOfMeasure;

[ApiController]
[Route("api/v1/masters/units-of-measure")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class UnitsOfMeasureController(
    UnitsOfMeasureService units,
    UnitsOfMeasureQueries queries) : ControllerBase
{
    [HttpGet(Name = "ListUnitsOfMeasure")]
    [RequirePermission(MastersPermissions.ReferenceDataRead)]
    [EndpointSummary("List units of measure")]
    [EndpointDescription("Includes each unit's decimal places and its conversion to the base unit of its family.")]
    [ProducesResponseType<PagedResult<UnitOfMeasureListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetUnitOfMeasureById")]
    [RequirePermission(MastersPermissions.ReferenceDataRead)]
    [EndpointSummary("Get one unit of measure")]
    [ProducesResponseType<UnitOfMeasureDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateUnitOfMeasure")]
    [RequirePermission(MastersPermissions.ReferenceDataCreate)]
    [EndpointSummary("Create a unit of measure")]
    [EndpointDescription(
        "Leave the base unit blank for a unit that is itself a base. A base unit must not "
        + "itself convert to another - conversion is one level, not a chain.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await units.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/units-of-measure/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateUnitOfMeasure")]
    [RequirePermission(MastersPermissions.ReferenceDataUpdate)]
    [EndpointSummary("Edit a unit of measure")]
    [EndpointDescription("The code cannot be changed: parts store the letters, not a key.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await units.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }
}

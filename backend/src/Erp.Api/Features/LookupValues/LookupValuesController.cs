using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.LookupValues;

[ApiController]
[Route("api/v1/masters/lookup-values")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class LookupValuesController(
    LookupValuesService lookupValues,
    LookupValuesQueries queries) : ControllerBase
{
    [HttpGet(Name = "ListLookupValues")]
    [RequirePermission(MastersPermissions.ReferenceDataRead)]
    [EndpointSummary("List reference-data options")]
    [EndpointDescription(
        "Every dropdown option in the system, across all lists. Filter on `type` to see one "
        + "list. Not the endpoint a form fills its dropdowns from - that is GET /masters/lookups.")]
    [ProducesResponseType<PagedResult<LookupValueListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetLookupValueById")]
    [RequirePermission(MastersPermissions.ReferenceDataRead)]
    [EndpointSummary("Get one reference-data option")]
    [ProducesResponseType<LookupValueDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateLookupValue")]
    [RequirePermission(MastersPermissions.ReferenceDataCreate)]
    [EndpointSummary("Add an option to a list")]
    [EndpointDescription("The option becomes selectable immediately - no deployment.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateLookupValueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lookupValues.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/lookup-values/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateLookupValue")]
    [RequirePermission(MastersPermissions.ReferenceDataUpdate)]
    [EndpointSummary("Rename, reorder or retire an option")]
    [EndpointDescription(
        "The list and the code cannot be changed: records store the code, so editing it would "
        + "reinterpret them. Retire an option by clearing Active - it stays for existing records "
        + "and drops out of the dropdown.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateLookupValueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lookupValues.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }
}

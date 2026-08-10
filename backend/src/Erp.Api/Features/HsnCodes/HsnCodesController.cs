using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.HsnCodes;

[ApiController]
[Route("api/v1/masters/hsn-codes")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class HsnCodesController(
    HsnCodesService hsnCodes,
    HsnCodesQueries queries) : ControllerBase
{
    [HttpGet(Name = "ListHsnCodes")]
    [RequirePermission(MastersPermissions.ReferenceDataRead)]
    [EndpointSummary("List HSN codes")]
    [EndpointDescription("Each row shows the GST rate in force today; the full rate history is on the detail.")]
    [ProducesResponseType<PagedResult<HsnCodeListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetHsnCodeById")]
    [RequirePermission(MastersPermissions.ReferenceDataRead)]
    [EndpointSummary("Get one HSN code and its rate history")]
    [ProducesResponseType<HsnCodeDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateHsnCode")]
    [RequirePermission(MastersPermissions.ReferenceDataCreate)]
    [EndpointSummary("Create an HSN code with its opening rate")]
    [EndpointDescription("The rate is required: a code with none would tax an invoice line at nothing.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateHsnCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await hsnCodes.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/hsn-codes/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateHsnCode")]
    [RequirePermission(MastersPermissions.ReferenceDataUpdate)]
    [EndpointSummary("Edit an HSN code's description or active flag")]
    [EndpointDescription("Neither the code nor its rates change here - post a rate to amend the tax.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateHsnCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await hsnCodes.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("{id:int}/rates", Name = "AddHsnGstRate")]
    [RequirePermission(MastersPermissions.ReferenceDataUpdate)]
    [EndpointSummary("Record a GST rate change")]
    [EndpointDescription(
        "Appends a rate from a date. Existing rates are never edited: a document keeps the "
        + "rate that applied when it was raised. Correct a wrong rate by superseding it.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> AddRate(
        int id,
        [FromBody] AddHsnGstRateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await hsnCodes.AddRateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }
}

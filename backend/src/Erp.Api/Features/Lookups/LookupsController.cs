using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.Lookups;

[ApiController]
[Route("api/v1/masters/lookups")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class LookupsController(LookupsService lookups) : ControllerBase
{
    [HttpGet(Name = "GetLookups")]
    [RequireAuthenticatedUser]
    [EndpointSummary("Option lists for master forms")]
    [EndpointDescription(
        "Comma-separated list names, e.g. ?types=uom,currency,supplier.type. Returns the "
        + "active options of each in display order. This is the only source of dropdown "
        + "options in the system - the web app holds none of its own.")]
    [ProducesResponseType<LookupSetDto>(StatusCodes.Status200OK)]
    public async Task<IResult> Get([FromQuery] string? types, CancellationToken cancellationToken)
    {
        var requested = (types ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
        var result = await lookups.GetAsync(requested, cancellationToken);

        return result.ToHttpResult();
    }
}

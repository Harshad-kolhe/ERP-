using Erp.Api.Common.Excel;
using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Api.Features.Imports;
using Erp.Contracts.Common;
using Erp.Contracts.Import;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.BusinessUnits;

[ApiController]
[Route("api/v1/masters/business-units")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class BusinessUnitsController(
    BusinessUnitsService businessUnits,
    BusinessUnitsQueries queries,
    BusinessUnitsImportService imports) : ControllerBase
{
    [HttpGet(Name = "ListBusinessUnits")]
    [RequirePermission(MastersPermissions.BusinessUnitRead)]
    [EndpointSummary("List business units")]
    [EndpointDescription(
        "Server-paged, with free-text search across business name, email and GSTN. "
        + "Returns every unit rather than the caller's own: this table defines the "
        + "tenancy boundary instead of sitting inside one, so the permission is the "
        + "only access control on it. pageSize is clamped to 200.")]
    [ProducesResponseType<PagedResult<BusinessUnitListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetBusinessUnitById")]
    [RequirePermission(MastersPermissions.BusinessUnitRead)]
    [EndpointSummary("Get one business unit")]
    [ProducesResponseType<BusinessUnitDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateBusinessUnit")]
    [RequirePermission(MastersPermissions.BusinessUnitCreate)]
    [EndpointSummary("Create a business unit")]
    [EndpointDescription("The unit id is the value every other table carries in its tenancy column.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateBusinessUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await businessUnits.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/business-units/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateBusinessUnit")]
    [RequirePermission(MastersPermissions.BusinessUnitUpdate)]
    [EndpointSummary("Update a business unit")]
    [EndpointDescription(
        "Requires the rowVersion returned by GET. The unit id cannot be changed — every "
        + "record in the system points at it.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateBusinessUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await businessUnits.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("import", Name = "Importbusiness-units")]
    [RequirePermission(MastersPermissions.BusinessUnitImport)]
    [EndpointSummary("Import business-units from an Excel file")]
    [EndpointDescription(
        "Accepts a single " + ImportLimits.FileExtension + " upload of at most "
        + "5,000 rows. All or nothing: every row is parsed and "
        + "checked first, and if anything is wrong nothing is written and the response "
        + "is 422 with every problem in the file. On success the response is 200 and "
        + "the report says how many rows landed.")]
    [ProducesResponseType<ImportResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ImportResultDto>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IResult> Import(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return ResultExtensions.ToProblem(ExcelErrors.Missing);
        }

        await using var content = file.OpenReadStream();

        var result = await imports.ImportAsync(
            new ImportFile(content, file.FileName, file.Length),
            cancellationToken);

        if (result.IsFailure)
        {
            return ResultExtensions.ToProblem(result.Error);
        }

        return result.Value.Committed
            ? Results.Ok(result.Value)
            : Results.Json(result.Value, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    [HttpGet("import-template", Name = "GetBusinessUnitsImportTemplate")]
    [RequirePermission(MastersPermissions.BusinessUnitImport)]
    [EndpointSummary("Download the business-units import template")]
    [EndpointDescription(
        "An empty workbook whose headings are exactly what the import expects, "
        + "plus a 'Column guide' sheet describing each column. Generated from the "
        + "same column definitions the importer parses, so the two cannot drift.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IResult GetImportTemplate() =>
        Results.File(
            BusinessUnitsImportService.BuildTemplate(),
            BusinessUnitsImportService.TemplateContentType,
            BusinessUnitsImportService.TemplateFileName);
}

using Erp.Api.Common.Excel;
using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Api.Features.Imports;
using Erp.Contracts.Common;
using Erp.Contracts.Import;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.Parts;

[ApiController]
[Route("api/v1/masters/parts")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class PartsController(
    PartsService parts,
    PartsQueries queries,
    PartsImportService imports) : ControllerBase
{
    [HttpGet(Name = "ListParts")]
    [RequirePermission(MastersPermissions.PartRead)]
    [EndpointSummary("List parts")]
    [EndpointDescription(
        "Server-paged. Supports sort=field:asc|desc (comma-separated), "
        + "filter=field:op:value (semicolon-separated), and free-text search across "
        + "part number, item code, description and HSN code. pageSize is clamped to 200.")]
    [ProducesResponseType<PagedResult<PartListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:guid}", Name = "GetPartById")]
    [RequirePermission(MastersPermissions.PartRead)]
    [EndpointSummary("Get a part")]
    [ProducesResponseType<PartDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreatePart")]
    [RequirePermission(MastersPermissions.PartCreate)]
    [EndpointSummary("Create a part")]
    [EndpointDescription("Creates the part in Draft status. It becomes usable once approved.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreatePartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await parts.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/parts/{id}", new { id }));
    }

    [HttpPut("{id:guid}", Name = "UpdatePart")]
    [RequirePermission(MastersPermissions.PartUpdate)]
    [EndpointSummary("Update a part")]
    [EndpointDescription(
        "Requires the rowVersion returned by GET. A stale value yields 409 rather than "
        + "overwriting a concurrent edit. The part number cannot be changed here.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        Guid id,
        [FromBody] UpdatePartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await parts.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("{id:guid}/approve", Name = "ApprovePart")]
    [RequirePermission(MastersPermissions.PartApprove)]
    [EndpointSummary("Approve a part")]
    [EndpointDescription("Approves a part awaiting review. The author cannot approve their own part.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await parts.ApproveAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("{id:guid}/submit", Name = "SubmitPartForApproval")]
    [RequirePermission(MastersPermissions.PartSubmit)]
    [EndpointSummary("Submit a part for approval")]
    [EndpointDescription("Moves a Draft part to PendingApproval. It cannot be edited while under review.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var result = await parts.SubmitAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("import", Name = "Importparts")]
    [RequirePermission(MastersPermissions.PartImport)]
    [EndpointSummary("Import parts from an Excel file")]
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

    [HttpGet("import-template", Name = "GetPartsImportTemplate")]
    [RequirePermission(MastersPermissions.PartImport)]
    [EndpointSummary("Download the parts import template")]
    [EndpointDescription(
        "An empty workbook whose headings are exactly what the import expects, "
        + "plus a 'Column guide' sheet describing each column. Generated from the "
        + "same column definitions the importer parses, so the two cannot drift.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IResult GetImportTemplate() =>
        Results.File(
            PartsImportService.BuildTemplate(),
            PartsImportService.TemplateContentType,
            PartsImportService.TemplateFileName);
}

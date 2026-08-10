using Erp.Api.Common.Excel;
using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Api.Features.Imports;
using Erp.Contracts.Common;
using Erp.Contracts.Import;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.Suppliers;

[ApiController]
[Route("api/v1/masters/suppliers")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class SuppliersController(
    SuppliersService suppliers,
    SuppliersQueries queries,
    SuppliersImportService imports) : ControllerBase
{
    [HttpGet(Name = "ListSuppliers")]
    [RequirePermission(MastersPermissions.SupplierRead)]
    [EndpointSummary("List suppliers")]
    [EndpointDescription(
        "Server-paged. Supports sort=field:asc|desc (comma-separated), "
        + "filter=field:op:value (semicolon-separated), and free-text search across "
        + "supplier code, name, email and GST number. pageSize is clamped to 200.")]
    [ProducesResponseType<PagedResult<SupplierListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetSupplierById")]
    [RequirePermission(MastersPermissions.SupplierRead)]
    [EndpointSummary("Get one supplier")]
    [EndpointDescription("Returns every editable field plus the rowVersion the update endpoint requires.")]
    [ProducesResponseType<SupplierDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateSupplier")]
    [RequirePermission(MastersPermissions.SupplierCreate)]
    [EndpointSummary("Create a supplier")]
    [EndpointDescription("Creates the supplier in the status supplied, defaulting to Draft.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var result = await suppliers.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/suppliers/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateSupplier")]
    [RequirePermission(MastersPermissions.SupplierUpdate)]
    [EndpointSummary("Update a supplier")]
    [EndpointDescription(
        "Requires the rowVersion returned by GET. A stale value yields 409 rather than "
        + "overwriting a concurrent edit. The supplier code cannot be changed here.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var result = await suppliers.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("import", Name = "Importsuppliers")]
    [RequirePermission(MastersPermissions.SupplierImport)]
    [EndpointSummary("Import suppliers from an Excel file")]
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

    [HttpGet("import-template", Name = "GetSuppliersImportTemplate")]
    [RequirePermission(MastersPermissions.SupplierImport)]
    [EndpointSummary("Download the suppliers import template")]
    [EndpointDescription(
        "An empty workbook whose headings are exactly what the import expects, "
        + "plus a 'Column guide' sheet describing each column. Generated from the "
        + "same column definitions the importer parses, so the two cannot drift.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IResult GetImportTemplate() =>
        Results.File(
            SuppliersImportService.BuildTemplate(),
            SuppliersImportService.TemplateContentType,
            SuppliersImportService.TemplateFileName);
}

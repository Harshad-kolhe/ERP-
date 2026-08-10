using Erp.Api.Common.Excel;
using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Api.Features.Imports;
using Erp.Contracts.Common;
using Erp.Contracts.Import;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.Employees;

[ApiController]
[Route("api/v1/masters/employees")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class EmployeesController(
    EmployeesService employees,
    EmployeesQueries queries,
    EmployeesImportService imports) : ControllerBase
{
    [HttpGet(Name = "ListEmployees")]
    [RequirePermission(MastersPermissions.EmployeeRead)]
    [EndpointSummary("List employees")]
    [EndpointDescription(
        "Server-paged. Supports sort=field:asc|desc (comma-separated), "
        + "filter=field:op:value (semicolon-separated), and free-text search across "
        + "first name, last name and email. Returns no payroll or credential field. "
        + "pageSize is clamped to 200.")]
    [ProducesResponseType<PagedResult<EmployeeListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetEmployeeById")]
    [RequirePermission(MastersPermissions.EmployeeRead)]
    [EndpointSummary("Get one employee")]
    [EndpointDescription(
        "Carries no credential. Pay fields are null unless the caller holds "
        + "masters.employee.payroll.read; canEditPayroll says which case it is.")]
    [ProducesResponseType<EmployeeDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateEmployee")]
    [RequirePermission(MastersPermissions.EmployeeCreate)]
    [EndpointSummary("Create an employee")]
    [EndpointDescription("Pay fields are ignored without masters.employee.payroll.read.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employees.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/employees/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateEmployee")]
    [RequirePermission(MastersPermissions.EmployeeUpdate)]
    [EndpointSummary("Update an employee")]
    [EndpointDescription(
        "Requires the rowVersion returned by GET. Pay fields are left untouched — not "
        + "cleared — for a caller without masters.employee.payroll.read.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employees.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("import", Name = "Importemployees")]
    [RequirePermission(MastersPermissions.EmployeeImport)]
    [EndpointSummary("Import employees from an Excel file")]
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

    [HttpGet("import-template", Name = "GetEmployeesImportTemplate")]
    [RequirePermission(MastersPermissions.EmployeeImport)]
    [EndpointSummary("Download the employees import template")]
    [EndpointDescription(
        "An empty workbook whose headings are exactly what the import expects, "
        + "plus a 'Column guide' sheet describing each column. Generated from the "
        + "same column definitions the importer parses, so the two cannot drift.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IResult GetImportTemplate() =>
        Results.File(
            EmployeesImportService.BuildTemplate(),
            EmployeesImportService.TemplateContentType,
            EmployeesImportService.TemplateFileName);
}

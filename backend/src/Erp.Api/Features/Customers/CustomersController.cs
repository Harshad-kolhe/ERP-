using Erp.Api.Common.Excel;
using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Api.Features.Imports;
using Erp.Contracts.Common;
using Erp.Contracts.Import;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.Customers;

[ApiController]
[Route("api/v1/masters/customers")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class CustomersController(
    CustomersService customers,
    CustomersQueries queries,
    CustomersImportService imports) : ControllerBase
{
    [HttpGet(Name = "ListCustomers")]
    [RequirePermission(MastersPermissions.CustomerRead)]
    [EndpointSummary("List customers")]
    [EndpointDescription(
        "Server-paged. Supports sort=field:asc|desc (comma-separated), "
        + "filter=field:op:value (semicolon-separated), and free-text search across "
        + "customer code, name, email and GST number. pageSize is clamped to 200.")]
    [ProducesResponseType<PagedResult<CustomerListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetCustomerById")]
    [RequirePermission(MastersPermissions.CustomerRead)]
    [EndpointSummary("Get one customer")]
    [EndpointDescription("Returns every editable field plus the rowVersion the update endpoint requires.")]
    [ProducesResponseType<CustomerDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateCustomer")]
    [RequirePermission(MastersPermissions.CustomerCreate)]
    [EndpointSummary("Create a customer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customers.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/customers/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateCustomer")]
    [RequirePermission(MastersPermissions.CustomerUpdate)]
    [EndpointSummary("Update a customer")]
    [EndpointDescription(
        "Requires the rowVersion returned by GET. A stale value yields 409. "
        + "The customer code cannot be changed here.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customers.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("import", Name = "Importcustomers")]
    [RequirePermission(MastersPermissions.CustomerImport)]
    [EndpointSummary("Import customers from an Excel file")]
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

    [HttpGet("import-template", Name = "GetCustomersImportTemplate")]
    [RequirePermission(MastersPermissions.CustomerImport)]
    [EndpointSummary("Download the customers import template")]
    [EndpointDescription(
        "An empty workbook whose headings are exactly what the import expects, "
        + "plus a 'Column guide' sheet describing each column. Generated from the "
        + "same column definitions the importer parses, so the two cannot drift.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IResult GetImportTemplate() =>
        Results.File(
            CustomersImportService.BuildTemplate(),
            CustomersImportService.TemplateContentType,
            CustomersImportService.TemplateFileName);
}

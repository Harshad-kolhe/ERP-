using Erp.Api.Common.Excel;
using Erp.Api.Common.Http;
using Erp.Api.Common.Security;
using Erp.Api.Features.Imports;
using Erp.Contracts.Common;
using Erp.Contracts.Import;
using Erp.Contracts.Masters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Features.Roles;

[ApiController]
[Route("api/v1/masters/roles")]
[Tags("Masters")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class RolesController(
    RolesService roles,
    RolesQueries queries,
    RolesImportService imports) : ControllerBase
{
    [HttpGet(Name = "ListMasterRoles")]
    [RequirePermission(MastersPermissions.RoleRead)]
    [EndpointSummary("List role master records")]
    [EndpointDescription(
        "The legacy role master, which does NOT grant permissions — authorisation runs on "
        + "Identity roles. Server-paged, with free-text search across the role name.")]
    [ProducesResponseType<PagedResult<RoleMasterListItemDto>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id:int}", Name = "GetRoleMasterById")]
    [RequirePermission(MastersPermissions.RoleRead)]
    [EndpointSummary("Get one legacy role master row")]
    [ProducesResponseType<RoleMasterDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await queries.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost(Name = "CreateRoleMaster")]
    [RequirePermission(MastersPermissions.RoleCreate)]
    [EndpointSummary("Create a legacy role master row")]
    [EndpointDescription("Grants no permissions. Permissions are assigned on the roles administration screen.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Create(
        [FromBody] CreateRoleMasterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roles.CreateAsync(request, cancellationToken);

        return result.ToHttpResult(id => Results.Created($"/api/v1/masters/roles/{id}", new { id }));
    }

    [HttpPut("{id:int}", Name = "UpdateRoleMaster")]
    [RequirePermission(MastersPermissions.RoleUpdate)]
    [EndpointSummary("Update a legacy role master row")]
    [EndpointDescription("Requires the rowVersion returned by GET. The role id cannot be changed.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IResult> Update(
        int id,
        [FromBody] UpdateRoleMasterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roles.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("import", Name = "Importroles")]
    [RequirePermission(MastersPermissions.RoleImport)]
    [EndpointSummary("Import roles from an Excel file")]
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

    [HttpGet("import-template", Name = "GetRolesImportTemplate")]
    [RequirePermission(MastersPermissions.RoleImport)]
    [EndpointSummary("Download the roles import template")]
    [EndpointDescription(
        "An empty workbook whose headings are exactly what the import expects, "
        + "plus a 'Column guide' sheet describing each column. Generated from the "
        + "same column definitions the importer parses, so the two cannot drift.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IResult GetImportTemplate() =>
        Results.File(
            RolesImportService.BuildTemplate(),
            RolesImportService.TemplateContentType,
            RolesImportService.TemplateFileName);
}

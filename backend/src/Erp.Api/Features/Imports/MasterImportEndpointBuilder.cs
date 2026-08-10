using Erp.Api.Common.Cqrs;
using Erp.Api.Common.Excel;
using Erp.Api.Common.Http;
using Erp.Api.Common.Modules;
using Erp.Contracts.Import;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Erp.Api.Features.Imports;

/// <summary>The uploaded file, as the application layer sees it â€” no <c>IFormFile</c>.</summary>
public sealed record ImportFile(Stream Content, string? FileName, long Length);

/// <summary>
/// The route shape every master import shares, in one place so the six of them
/// cannot describe themselves six different ways in the OpenAPI document.
/// </summary>
public static class MasterImportEndpointBuilder
{
    /// <summary>
    /// Maps <c>GET /{resource}/import-template</c>.
    /// <para>
    /// Gated on the same permission as the import itself. The template names every
    /// field the master holds, including the ones the grid hides, so it is a
    /// description of the schema rather than a blank form.
    /// </para>
    /// </summary>
    public static void MapTemplate(
        RouteGroupBuilder group,
        string resource,
        string sheetName,
        string permission,
        IReadOnlyList<ImportColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet($"/{resource}/import-template", () => Results.File(
                ExcelTemplateWriter.Build(sheetName, columns),
                ExcelContentType,
                $"{sheetName}-import-template{ImportLimits.FileExtension}"))
            .WithName($"Get{sheetName}ImportTemplate")
            .WithSummary($"Download the {resource} import template")
            .WithDescription(
                "An empty workbook whose headings are exactly what the import expects, "
                + "plus a 'Column guide' sheet describing each column. Generated from the "
                + "same column definitions the importer parses, so the two cannot drift.")
            .RequirePermission(permission)
            .Produces(StatusCodes.Status200OK, contentType: ExcelContentType);
    }

    /// <summary>
    /// Maps <c>POST /{resource}/import</c>.
    /// <para>
    /// Generic over the command so the handler arrives through the delegate's
    /// parameters and is injected by DI. Resolving it from an
    /// <see cref="IServiceProvider"/> would be shorter and is banned in this
    /// codebase â€” a hidden dependency is how a legacy controller reached 41
    /// constructor parameters without anyone noticing.
    /// </para>
    /// <para>
    /// The status code is the other thing worth reading here. A rejected file comes
    /// back as <c>422</c> carrying the full <see cref="ImportResultDto"/>, not a
    /// <c>200</c> with a failure flag inside â€” that shape is exactly what this
    /// system exists to stop doing, because every proxy and dashboard then reads a
    /// failure as success. Nor is it a bare problem response: the per-row report is
    /// the point of the call, and the operator needs it far more than a title.
    /// </para>
    /// </summary>
    public static void MapImport<TCommand>(
        RouteGroupBuilder group,
        string resource,
        string permission,
        Func<ImportFile, TCommand> toCommand)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(toCommand);

        group.MapPost($"/{resource}/import", async (
                IFormFile? file,
                ICommandHandler<TCommand, ImportResultDto> handler,
                CancellationToken cancellationToken) =>
            {
                if (file is null)
                {
                    return ResultExtensions.ToProblem(ExcelErrors.Missing);
                }

                await using var content = file.OpenReadStream();

                var command = toCommand(new ImportFile(content, file.FileName, file.Length));
                var result = await handler.HandleAsync(command, cancellationToken);

                if (result.IsFailure)
                {
                    // The file never got as far as being read row by row â€” wrong
                    // type, unopenable, missing columns. There is no report to give.
                    return ResultExtensions.ToProblem(result.Error);
                }

                return result.Value.Committed
                    ? Results.Ok(result.Value)
                    : Results.Json(result.Value, statusCode: StatusCodes.Status422UnprocessableEntity);
            })
            .WithName($"Import{resource}")
            .WithSummary($"Import {resource} from an Excel file")
            .WithDescription(
                $"Accepts a single {ImportLimits.FileExtension} upload of at most "
                + $"{ImportLimits.MaxRows:N0} rows. All or nothing: every row is parsed and "
                + "checked first, and if anything is wrong nothing is written and the response "
                + "is 422 with every problem in the file. On success the response is 200 and "
                + "the report says how many rows landed.")
            .RequirePermission(permission)
            // Minimal APIs require anti-forgery to be opted out of for multipart
            // uploads. Safe here: this is a bearer-token API with no cookie
            // authentication, so there is no ambient credential a cross-site form
            // post could ride on.
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportResultDto>()
            .Produces<ImportResultDto>(StatusCodes.Status422UnprocessableEntity);
    }

    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

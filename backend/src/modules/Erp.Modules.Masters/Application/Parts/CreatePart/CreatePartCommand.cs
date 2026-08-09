using Erp.Contracts.Masters;

namespace Erp.Modules.Masters.Application.Parts.CreatePart;

internal sealed record CreatePartCommand(
    string PartNumber,
    string Description,
    Guid? CategoryId,
    string UnitOfMeasureCode,
    string? HsnCode,
    string? DrawingNumber,
    PartAttributesDto? Attributes);

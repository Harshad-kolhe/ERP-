using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Modules.Masters.Domain.Parts;
using Erp.Modules.Masters.Infrastructure;
using Erp.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Parts.SubmitPart;

internal sealed record SubmitPartCommand(Guid Id);

internal sealed class SubmitPartHandler(MastersDbContext db) : ICommandHandler<SubmitPartCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(SubmitPartCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var partId = new PartId(command.Id);

        var part = await db.Parts.FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part is null)
        {
            return Result.Failure<Unit>(PartErrors.NotFound(command.Id));
        }

        var submitted = part.SubmitForApproval();

        if (submitted.IsFailure)
        {
            return Result.Failure<Unit>(submitted.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}

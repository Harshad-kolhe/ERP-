using Erp.BuildingBlocks.Application.Abstractions;
using Erp.BuildingBlocks.Application.Cqrs;
using Erp.Persistence;
using Erp.Persistence.Domain.Parts;
using Erp.SharedKernel.Results;
using Erp.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Masters.Application.Parts.ApprovePart;

internal sealed record ApprovePartCommand(Guid Id);

internal sealed class ApprovePartHandler(
    ErpDbContext db,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<ApprovePartCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(ApprovePartCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var partId = new PartId(command.Id);

        var part = await db.Parts.FirstOrDefaultAsync(p => p.Id == partId, cancellationToken);

        if (part is null)
        {
            return Result.Failure<Unit>(PartErrors.NotFound(command.Id));
        }

        // Both the approver and the timestamp are passed in rather than read from
        // ambient state, which keeps the segregation-of-duties rule inside the
        // aggregate and directly unit-testable.
        var approved = part.Approve(currentUser.UserId, clock.UtcNow);

        if (approved.IsFailure)
        {
            return Result.Failure<Unit>(approved.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}

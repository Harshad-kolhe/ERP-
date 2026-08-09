using Erp.Persistence.Domain.Parts;

namespace Erp.Modules.Masters.Tests.Domain;

/// <summary>
/// The part lifecycle rules, tested directly against the aggregate.
/// <para>
/// No database, no web host, no mocks — because the rules live in the domain
/// class rather than in a service that owns a DbContext. That is the practical
/// payoff of the boundary: these run in milliseconds and describe the business
/// rules in a form someone can read. The system this replaces had 320,000 lines
/// and no test project at all, so no rule was written down anywhere executable.
/// </para>
/// </summary>
public sealed class PartTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 8, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Author = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Approver = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_normalises_the_part_number_and_unit_code()
    {
        var part = NewPart(partNumber: "  mtr-100  ", unitOfMeasure: " nos ");

        part.PartNumber.ShouldBe("MTR-100");
        part.UnitOfMeasureCode.ShouldBe("NOS");
    }

    [Fact]
    public void Create_starts_in_draft()
    {
        NewPart().Status.ShouldBe(PartStatus.Draft);
    }

    [Fact]
    public void Submitting_a_draft_moves_it_to_pending_approval()
    {
        var part = NewPart();

        part.SubmitForApproval().IsSuccess.ShouldBeTrue();
        part.Status.ShouldBe(PartStatus.PendingApproval);
    }

    [Fact]
    public void Submitting_twice_is_rejected()
    {
        var part = NewPart();
        part.SubmitForApproval();

        var second = part.SubmitForApproval();

        second.IsFailure.ShouldBeTrue();
        second.Error.Code.ShouldBe("part.cannot_submit");
    }

    /// <summary>
    /// Prevents the submitter changing what the approver is looking at between
    /// reading it and approving it.
    /// </summary>
    [Fact]
    public void A_part_cannot_be_edited_while_awaiting_approval()
    {
        var part = NewPart();
        part.SubmitForApproval();

        var result = part.Update("Changed behind the approver's back", null, "NOS", null, null);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("part.not_editable_pending_approval");
        part.Description.ShouldBe("Drive motor");
    }

    /// <summary>Segregation of duties: nobody waves through their own record.</summary>
    [Fact]
    public void The_author_cannot_approve_their_own_part()
    {
        var part = NewPart();
        part.CreatedByUserId = Author;
        part.SubmitForApproval();

        var result = part.Approve(Author, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("part.approver_is_author");
        part.Status.ShouldBe(PartStatus.PendingApproval);
    }

    [Fact]
    public void Approving_a_pending_part_succeeds_and_raises_an_event()
    {
        var part = NewPart();
        part.CreatedByUserId = Author;
        part.SubmitForApproval();

        var result = part.Approve(Approver, Now);

        result.IsSuccess.ShouldBeTrue();
        part.Status.ShouldBe(PartStatus.Approved);

        var raised = part.DomainEvents.OfType<PartApproved>().ShouldHaveSingleItem();
        raised.ApprovedByUserId.ShouldBe(Approver);

        // The timestamp is the one the caller passed, not an ambient clock read.
        raised.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void A_draft_cannot_be_approved_without_being_submitted()
    {
        var part = NewPart();
        part.CreatedByUserId = Author;

        var result = part.Approve(Approver, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("part.cannot_approve");
    }

    [Fact]
    public void An_approved_part_can_still_be_edited()
    {
        var part = NewPart();
        part.CreatedByUserId = Author;
        part.SubmitForApproval();
        part.Approve(Approver, Now);

        var result = part.Update("Drive motor, 3 kW", null, "NOS", "8501", null);

        result.IsSuccess.ShouldBeTrue();
        part.HsnCode.ShouldBe("8501");
        part.Status.ShouldBe(PartStatus.Approved);
    }

    [Fact]
    public void Blank_optional_fields_are_stored_as_null_not_empty_strings()
    {
        var part = NewPart();

        part.Update("Drive motor", null, "NOS", "   ", "  ");

        part.HsnCode.ShouldBeNull();
        part.DrawingNumber.ShouldBeNull();
    }

    /// <summary>
    /// The rejection state the legacy flow could not express. It stored
    /// RejectedBy/On/Reason and then set the status back to pending, so the grid
    /// showed a part still waiting and the reason was invisible on screen.
    /// </summary>
    [Fact]
    public void A_submitted_part_can_be_rejected_with_a_reason()
    {
        var part = NewPart();
        part.SubmitForApproval();

        var result = part.Reject("Drawing does not match the specification");

        result.IsSuccess.ShouldBeTrue();
        part.Status.ShouldBe(PartStatus.Rejected);
        part.RevisionRemark.ShouldBe("Drawing does not match the specification");
    }

    [Fact]
    public void Only_a_submitted_part_can_be_rejected()
    {
        var rejected = NewPart().Reject("No");

        rejected.IsFailure.ShouldBeTrue();
        rejected.Error.Code.ShouldBe("part.cannot_reject");
    }

    [Fact]
    public void An_approved_part_can_be_held_and_released()
    {
        var part = NewPart();
        part.SubmitForApproval();
        part.Approve(Approver, Now);

        part.Hold("Supplier quality issue").IsSuccess.ShouldBeTrue();
        part.Status.ShouldBe(PartStatus.Hold);
        part.HoldRemark.ShouldBe("Supplier quality issue");

        // Releasing returns it to approved rather than through approval again —
        // a hold pauses accepted work, it does not undo the acceptance.
        part.Release().IsSuccess.ShouldBeTrue();
        part.Status.ShouldBe(PartStatus.Approved);
    }

    [Fact]
    public void Only_an_approved_part_can_be_held()
    {
        var held = NewPart().Hold("Too early");

        held.IsFailure.ShouldBeTrue();
        held.Error.Code.ShouldBe("part.cannot_hold");
    }

    /// <summary>
    /// The whole reason Inactive was retired as a status: withdrawing a part used
    /// to overwrite its approval state, so nothing recorded that it had ever been
    /// approved and reactivating it meant guessing where to put it back.
    /// </summary>
    [Fact]
    public void Withdrawing_a_part_leaves_its_approval_state_alone()
    {
        var part = NewPart();
        part.SubmitForApproval();
        part.Approve(Approver, Now);

        part.Deactivate("Superseded by MTR-200");

        part.IsActive.ShouldBeFalse();
        part.Status.ShouldBe(PartStatus.Approved);
        part.InactiveRemark.ShouldBe("Superseded by MTR-200");

        part.Reactivate();

        part.IsActive.ShouldBeTrue();
        part.Status.ShouldBe(PartStatus.Approved);
    }

    private static Part NewPart(string partNumber = "MTR-100", string unitOfMeasure = "NOS") =>
        Part.Create(partNumber, "Drive motor", null, unitOfMeasure, null, null);
}

using Erp.Modules.Masters.Domain.Parts;

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

    private static Part NewPart(string partNumber = "MTR-100", string unitOfMeasure = "NOS") =>
        Part.Create(partNumber, "Drive motor", null, unitOfMeasure, null, null);
}

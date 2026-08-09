using Erp.SharedKernel.Primitives;
using Erp.SharedKernel.Results;

namespace Erp.Persistence.Domain.Parts;

/// <summary>
/// A part master record — the atom the whole system is built on. Every BOM line,
/// purchase order line, stock ledger entry and job card ultimately points here.
/// <para>
/// State transitions live on this class, not in a service and not in a stored
/// procedure. The legacy equivalent kept its rules in a 10,444-line
/// <c>BomBLL</c> and ~144 stored procedures that were not in source control, so
/// nobody could answer "what is allowed to happen to a part?" without reading the
/// database. Here the answer is this file.
/// </para>
/// <para>
/// The type is <c>internal</c>: outside Masters, a part is only ever a
/// <c>PartDto</c> or an id. No other module can take a dependency on this shape.
/// </para>
/// </summary>
public sealed class Part : AggregateRoot<PartId>, IAuditable, ISoftDeletable, IBusinessUnitScoped, IHasRowVersion
{
    /// <summary>For EF materialisation only.</summary>
    private Part()
    {
    }

    private Part(
        PartId id,
        string partNumber,
        string description,
        Guid? categoryId,
        string unitOfMeasureCode,
        string? hsnCode,
        string? drawingNumber)
        : base(id)
    {
        PartNumber = partNumber;
        OriginalPartNumber = partNumber;
        Description = description;
        CategoryId = categoryId;
        UnitOfMeasureCode = unitOfMeasureCode;
        HsnCode = hsnCode;
        DrawingNumber = drawingNumber;
        Status = PartStatus.Draft;
    }

    /// <summary>
    /// Business key. Unique per business unit, and never changed by an ordinary edit.
    /// This is the legacy <c>SysPartNumber</c> — the number the system issues. The
    /// manually assigned code lives in <see cref="ItemNumber"/>.
    /// </summary>
    public string PartNumber { get; private set; } = null!;

    /// <summary>
    /// The part number this one is a revision of — the legacy <c>OriginalPartNumber</c>.
    /// <para>
    /// In the legacy scheme a part number ends in a two-digit revision
    /// (<c>MS-RAW-000123-<b>00</b></c>), and each revision is a separate row. This
    /// column is what ties them together: every revision carries the number the
    /// first one was issued under. Without it a migrated master is a flat list in
    /// which <c>-00</c> and <c>-01</c> look like unrelated parts, and the link
    /// cannot be reconstructed afterwards because the number format is not reliably
    /// parseable across series.
    /// </para>
    /// <para>
    /// Set to <see cref="PartNumber"/> for a part created here, so the field is
    /// never null for a part this system issued.
    /// </para>
    /// </summary>
    public string OriginalPartNumber { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public Guid? CategoryId { get; private set; }

    /// <summary>The primary unit of measure. Legacy <c>PrimaryUoM</c>.</summary>
    public string UnitOfMeasureCode { get; private set; } = null!;

    /// <summary>HSN code, for GST classification. Legacy <c>HSCode</c>.</summary>
    public string? HsnCode { get; private set; }

    /// <summary>Path to the current drawing revision. Legacy <c>DrawingNumber</c>.</summary>
    public string? DrawingNumber { get; private set; }

    public PartStatus Status { get; private set; }

    /// <summary>
    /// Whether the part may be used on new documents.
    /// <para>
    /// Separate from <see cref="Status"/>, and deliberately so — the two answer
    /// different questions. <see cref="Status"/> is where the record sits in the
    /// approval workflow; this is whether the business still transacts on the part.
    /// A part can be approved and withdrawn at the same time, which one field cannot
    /// express. The legacy grid showed both columns for exactly this reason.
    /// </para>
    /// </summary>
    public bool IsActive { get; private set; } = true;

    // ---- Legacy Part Master attributes. See PartAttributes for the field-by-field
    // mapping; they are set together through Create/Update rather than individually,
    // so there is one place where a part's descriptive state changes.

    public string? ItemNumber { get; private set; }

    public string? TechnicalSpecification { get; private set; }

    public string? Moc { get; private set; }

    public string? PartCategoryCode { get; private set; }

    public string? PartType { get; private set; }

    public string? FormCategory { get; private set; }

    public string? PurchaseUomCode { get; private set; }

    public string? SellingUomCode { get; private set; }

    public string? MaterialType { get; private set; }

    public string? SeriesCode { get; private set; }

    public string? PartRevisionNo { get; private set; }

    public string? SourceCode { get; private set; }

    public decimal? WeightKg { get; private set; }

    public int? LeadTimeDays { get; private set; }

    public decimal? MinimumStockLevel { get; private set; }

    public int? ReorderPoint { get; private set; }

    public string? RevisionRemark { get; private set; }

    public string? HoldRemark { get; private set; }

    public string? InactiveRemark { get; private set; }

    public int BusinessUnitId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Creates a draft part. Uniqueness of the part number is checked by the
    /// handler, which is the only layer that can see other rows.
    /// </summary>
    /// <param name="attributes">
    /// The descriptive fields. Optional so a part can be created from its identity
    /// alone — the legacy screen fills most of these in later, and requiring them up
    /// front would make the common "raise the number now, specify it after the design
    /// review" path impossible.
    /// </param>
    /// <param name="originalPartNumber">
    /// Only supplied when importing: a legacy revision must keep pointing at the
    /// number its family was issued under. Left null — every path except import —
    /// the part is its own original, which is what a newly issued number means.
    /// </param>
    public static Part Create(
        string partNumber,
        string description,
        Guid? categoryId,
        string unitOfMeasureCode,
        string? hsnCode,
        string? drawingNumber,
        PartAttributes? attributes = null,
        string? originalPartNumber = null)
    {
        var part = new Part(
            PartId.New(),
            Normalize(partNumber),
            description.Trim(),
            categoryId,
            Normalize(unitOfMeasureCode),
            string.IsNullOrWhiteSpace(hsnCode) ? null : Normalize(hsnCode),
            string.IsNullOrWhiteSpace(drawingNumber) ? null : drawingNumber.Trim());

        if (!string.IsNullOrWhiteSpace(originalPartNumber))
        {
            part.OriginalPartNumber = Normalize(originalPartNumber);
        }

        part.Apply(attributes ?? new PartAttributes());

        return part;
    }

    public Result Update(
        string description,
        Guid? categoryId,
        string unitOfMeasureCode,
        string? hsnCode,
        string? drawingNumber,
        PartAttributes? attributes = null)
    {
        // Editing during review would let the submitter change what the approver
        // is looking at between reading it and approving it.
        if (Status == PartStatus.PendingApproval)
        {
            return Result.Failure(PartErrors.NotEditableWhilePendingApproval);
        }

        Description = description.Trim();
        CategoryId = categoryId;
        UnitOfMeasureCode = Normalize(unitOfMeasureCode);
        HsnCode = string.IsNullOrWhiteSpace(hsnCode) ? null : Normalize(hsnCode);
        DrawingNumber = string.IsNullOrWhiteSpace(drawingNumber) ? null : drawingNumber.Trim();

        Apply(attributes ?? new PartAttributes());

        return Result.Success();
    }

    public Result SubmitForApproval()
    {
        if (Status != PartStatus.Draft)
        {
            return Result.Failure(PartErrors.CannotSubmitFromStatus(Status));
        }

        Status = PartStatus.PendingApproval;
        return Result.Success();
    }

    /// <param name="approverUserId">Who is approving.</param>
    /// <param name="occurredAtUtc">
    /// Supplied by the caller rather than read from the ambient clock, which keeps
    /// this class free of I/O and lets tests drive time directly.
    /// </param>
    public Result Approve(Guid approverUserId, DateTimeOffset occurredAtUtc)
    {
        if (Status != PartStatus.PendingApproval)
        {
            return Result.Failure(PartErrors.CannotApproveFromStatus(Status));
        }

        // Segregation of duties: the author cannot wave through their own record.
        if (approverUserId == CreatedByUserId)
        {
            return Result.Failure(PartErrors.ApproverCannotBeAuthor);
        }

        Status = PartStatus.Approved;

        Raise(new PartApproved(
            Guid.CreateVersion7(),
            occurredAtUtc,
            Id,
            PartNumber,
            approverUserId));

        return Result.Success();
    }

    /// <summary>
    /// Withdraws the part from new documents, leaving its approval state alone.
    /// <para>
    /// It used to also set the status to <c>Inactive</c>, which destroyed the very
    /// thing the two fields exist to keep apart: after withdrawing an approved
    /// part, nothing recorded that it had ever been approved, so reactivating it
    /// meant guessing where it should go back to. Withdrawal and approval are
    /// different questions, and this answers only one of them.
    /// </para>
    /// </summary>
    /// <param name="reason">
    /// Recorded against the part. The legacy grid has an "Inactive Remark" column
    /// that was filled in by hand and therefore often was not; taking it here means
    /// a part cannot go out of use without the reason going with it.
    /// </param>
    public Result Deactivate(string? reason = null)
    {
        IsActive = false;
        InactiveRemark = Clean(reason) ?? InactiveRemark;

        return Result.Success();
    }

    /// <summary>Puts a withdrawn part back into use. Its approval state is untouched.</summary>
    public Result Reactivate()
    {
        IsActive = true;

        return Result.Success();
    }

    /// <summary>
    /// Sends a submitted part back, with the reason it was refused.
    /// <para>
    /// A state of its own rather than a return to <see cref="PartStatus.Draft"/>.
    /// The legacy flow stored the rejection and then reset the status, so the grid
    /// showed a part waiting for approval again and the reason was invisible unless
    /// somebody read the table.
    /// </para>
    /// </summary>
    public Result Reject(string reason)
    {
        if (Status != PartStatus.PendingApproval)
        {
            return Result.Failure(PartErrors.CannotRejectFromStatus(Status));
        }

        Status = PartStatus.Rejected;
        RevisionRemark = Clean(reason) ?? RevisionRemark;

        return Result.Success();
    }

    /// <summary>
    /// Pauses an approved part — legacy status <c>10</c>. It stays approved work,
    /// so releasing it returns it to <see cref="PartStatus.Approved"/> rather than
    /// through the whole approval again.
    /// </summary>
    public Result Hold(string reason)
    {
        if (Status != PartStatus.Approved)
        {
            return Result.Failure(PartErrors.CannotHoldFromStatus(Status));
        }

        Status = PartStatus.Hold;
        HoldRemark = Clean(reason) ?? HoldRemark;

        return Result.Success();
    }

    /// <summary>Lifts a hold, returning the part to the approved state it was paused from.</summary>
    public Result Release()
    {
        if (Status != PartStatus.Hold)
        {
            return Result.Failure(PartErrors.CannotReleaseFromStatus(Status));
        }

        Status = PartStatus.Approved;

        return Result.Success();
    }

    /// <summary>
    /// Puts a part straight into the lifecycle state its source record was in,
    /// without running the transitions that would normally get it there.
    /// <para>
    /// For the importer, and nothing else. Legacy rows arrive already approved or
    /// already withdrawn, and replaying <see cref="SubmitForApproval"/> then
    /// <see cref="Approve"/> against them would be a lie: it would stamp a
    /// present-day approver onto a decision somebody else made years ago, and
    /// <see cref="Approve"/> would reject half the file anyway because the importing
    /// user is recorded as the author.
    /// </para>
    /// <para>
    /// The rules are therefore bypassed deliberately and visibly, rather than being
    /// quietly loosened for everyone — which is what adding an <c>isImport</c> flag
    /// to <see cref="Approve"/> would have done.
    /// </para>
    /// </summary>
    public void RestoreLifecycleState(PartStatus status, bool isActive)
    {
        Status = status;
        IsActive = isActive;
    }

    /// <summary>
    /// Copies the descriptive fields in, normalising as it goes.
    /// <para>
    /// Codes are upper-cased for the same reason part numbers are — see
    /// <see cref="Normalize"/>. Free text is only trimmed: upper-casing a technical
    /// specification would destroy the case of the unit symbols in it.
    /// </para>
    /// </summary>
    private void Apply(PartAttributes attributes)
    {
        ItemNumber = Clean(attributes.ItemNumber);
        TechnicalSpecification = Clean(attributes.TechnicalSpecification);
        Moc = CleanCode(attributes.Moc);
        PartCategoryCode = CleanCode(attributes.PartCategoryCode);
        PartType = Clean(attributes.PartType);
        FormCategory = Clean(attributes.FormCategory);
        PurchaseUomCode = CleanCode(attributes.PurchaseUomCode);
        SellingUomCode = CleanCode(attributes.SellingUomCode);
        MaterialType = Clean(attributes.MaterialType);
        SeriesCode = CleanCode(attributes.SeriesCode);
        PartRevisionNo = CleanCode(attributes.PartRevisionNo);
        SourceCode = Clean(attributes.SourceCode);
        WeightKg = attributes.WeightKg;
        LeadTimeDays = attributes.LeadTimeDays;
        MinimumStockLevel = attributes.MinimumStockLevel;
        ReorderPoint = attributes.ReorderPoint;
        RevisionRemark = Clean(attributes.RevisionRemark);
        HoldRemark = Clean(attributes.HoldRemark);
        InactiveRemark = Clean(attributes.InactiveRemark);
    }

    /// <summary>
    /// Upper-cases and trims codes so <c>"mtr"</c>, <c>"MTR "</c> and <c>"Mtr"</c>
    /// cannot become three different parts — a duplicate-master problem that is
    /// cheap to prevent here and expensive to unpick later.
    /// </summary>
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    /// <summary>Trims free text, collapsing blank input to null so "" and null are not two states.</summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>As <see cref="Clean"/>, then <see cref="Normalize"/> for values used as codes.</summary>
    private static string? CleanCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}

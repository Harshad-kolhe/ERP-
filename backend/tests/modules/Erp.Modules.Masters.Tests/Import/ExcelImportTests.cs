using ClosedXML.Excel;
using Erp.BuildingBlocks.Excel;

namespace Erp.Modules.Masters.Tests.Import;

/// <summary>
/// The reading half of the importer, tested against real workbooks.
/// <para>
/// Real <c>.xlsx</c> bytes rather than a stubbed reader, because almost everything
/// that goes wrong with an import is a property of actual spreadsheets — a header
/// with a stray space, a code Excel decided was a number, a date formatted in
/// somebody else's locale. A fake reader would agree with whatever the parser
/// believed and prove nothing.
/// </para>
/// </summary>
public sealed class ExcelImportTests
{
    private static readonly ImportColumn Code = new("Code", Required: true, MaxLength: 5);
    private static readonly ImportColumn Quantity = new("Quantity", ImportColumnKind.WholeNumber);
    private static readonly ImportColumn Weight = new("Weight", ImportColumnKind.Number);
    private static readonly ImportColumn Active = new("Active", ImportColumnKind.Boolean);
    private static readonly ImportColumn Joined = new("Joined", ImportColumnKind.Date);
    private static readonly ImportColumn Skills = new("Skills", ImportColumnKind.TextList);

    [Fact]
    public void Headers_match_regardless_of_case_and_spacing()
    {
        // The three spellings a hand-edited sheet actually contains.
        var sheet = Read(["  code  "], [["A1"]]);

        var reader = new ImportRowReader(sheet.Rows[0]);

        reader.Text(Code).ShouldBe("A1");
    }

    [Fact]
    public void Row_numbers_are_the_ones_excel_shows()
    {
        var sheet = Read(["Code"], [["A1"], ["A2"]]);

        // Row 1 is the header, so the first data row is 2 — the number the operator
        // sees in the gutter and the number the error report must quote.
        sheet.Rows[0].Row.ShouldBe(2);
        sheet.Rows[1].Row.ShouldBe(3);
    }

    [Fact]
    public void Blank_rows_are_skipped_rather_than_rejected()
    {
        var sheet = Read(["Code"], [["A1"], [""], ["A2"]]);

        sheet.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public void A_missing_required_column_fails_the_whole_file()
    {
        var sheet = Read(["Quantity"], [["1"]]);

        var result = ImportSheetBinder.RequireColumns(sheet, [Code, Quantity]);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("import.file.missing-columns");
    }

    [Fact]
    public void A_duplicate_heading_fails_the_whole_file()
    {
        var result = ExcelSheetReader.Read(Workbook(["Code", "code"], [["A", "B"]]));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("import.file.duplicate-header");
    }

    /// <summary>
    /// The row carries a value in another column on purpose. A row that is blank
    /// all the way across is skipped rather than reported — see
    /// <see cref="Blank_rows_are_skipped_rather_than_rejected"/> — so a row with
    /// content is what actually exercises the required-cell check.
    /// </summary>
    [Fact]
    public void A_blank_required_cell_is_reported_once_against_its_column()
    {
        var sheet = Read(["Code", "Quantity"], [["", "5"]]);
        var reader = new ImportRowReader(sheet.Rows[0]);

        reader.Text(Code);

        reader.IsValid.ShouldBeFalse();
        reader.Errors.Count.ShouldBe(1);
        reader.Errors[0].Column.ShouldBe("Code");
        reader.Errors[0].Row.ShouldBe(2);
    }

    [Fact]
    public void An_over_long_value_names_the_column_and_the_limit()
    {
        var sheet = Read(["Code"], [["ABCDEFGH"]]);
        var reader = new ImportRowReader(sheet.Rows[0]);

        reader.Text(Code);

        reader.Errors.ShouldHaveSingleItem();
        reader.Errors[0].Message.ShouldContain("5 characters or fewer");
    }

    /// <summary>
    /// The point of the row reader: one upload reports every problem in the row, so
    /// the operator fixes them together instead of discovering them one at a time.
    /// </summary>
    [Fact]
    public void Every_bad_cell_in_a_row_is_reported_not_just_the_first()
    {
        var sheet = Read(
            ["Code", "Quantity", "Weight", "Active"],
            [["", "many", "heavy", "perhaps"]]);

        var reader = new ImportRowReader(sheet.Rows[0]);

        reader.Text(Code);
        reader.WholeNumber(Quantity);
        reader.Number(Weight);
        reader.Boolean(Active);

        reader.Errors.Count.ShouldBe(4);
    }

    [Fact]
    public void A_whole_number_column_accepts_a_formatted_decimal_but_not_a_real_fraction()
    {
        var sheet = Read(["Quantity"], [["12.00"], ["12.5"]]);

        var whole = new ImportRowReader(sheet.Rows[0]);
        whole.WholeNumber(Quantity).ShouldBe(12);
        whole.IsValid.ShouldBeTrue();

        var fractional = new ImportRowReader(sheet.Rows[1]);
        fractional.WholeNumber(Quantity);
        fractional.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Yes_no_true_false_and_one_zero_all_read_as_booleans()
    {
        var sheet = Read(["Active"], [["Yes"], ["NO"], ["true"], ["0"]]);

        new ImportRowReader(sheet.Rows[0]).Boolean(Active).ShouldBe(true);
        new ImportRowReader(sheet.Rows[1]).Boolean(Active).ShouldBe(false);
        new ImportRowReader(sheet.Rows[2]).Boolean(Active).ShouldBe(true);
        new ImportRowReader(sheet.Rows[3]).Boolean(Active).ShouldBe(false);
    }

    /// <summary>
    /// A real date cell displays according to the author's locale. Reading the
    /// display text would make 03/04/2026 mean March in one office and April in
    /// another, so the reader takes the underlying date instead.
    /// </summary>
    [Fact]
    public void A_real_date_cell_is_read_from_its_value_not_its_display_format()
    {
        using var stream = new MemoryStream();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.AddWorksheet("Sheet1");
            worksheet.Cell(1, 1).Value = "Joined";

            var cell = worksheet.Cell(2, 1);
            cell.Value = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);
            cell.Style.DateFormat.Format = "MM/dd/yyyy";

            workbook.SaveAs(stream);
        }

        stream.Position = 0;

        var sheet = ExcelSheetReader.Read(stream).Value;
        var reader = new ImportRowReader(sheet.Rows[0]);

        reader.Date(Joined).ShouldBe(new DateTimeOffset(2026, 4, 3, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_comma_separated_cell_becomes_a_trimmed_list()
    {
        var sheet = Read(["Skills"], [[" Welding , Turning ,, Assembly "]]);
        var reader = new ImportRowReader(sheet.Rows[0]);

        reader.TextList(Skills).ShouldBe(["Welding", "Turning", "Assembly"]);
    }

    /// <summary>
    /// An optional column absent from the sheet is not an error — an operator may
    /// upload only the columns they care about.
    /// </summary>
    [Fact]
    public void An_absent_optional_column_reads_as_blank()
    {
        var sheet = Read(["Code"], [["A1"]]);
        var reader = new ImportRowReader(sheet.Rows[0]);

        reader.Number(Weight).ShouldBeNull();
        reader.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void The_template_writes_one_column_per_declaration_plus_a_guide_sheet()
    {
        var bytes = ExcelTemplateWriter.Build("Things", [Code, Quantity, Weight]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));

        workbook.Worksheets.Count.ShouldBe(2);
        workbook.Worksheet("Things").Cell(1, 1).GetString().ShouldBe("Code");
        workbook.Worksheet("Things").Cell(1, 3).GetString().ShouldBe("Weight");
        workbook.Worksheets.Contains("Column guide").ShouldBeTrue();
    }

    /// <summary>
    /// The property that makes the template trustworthy: a sheet produced from the
    /// declarations parses against those same declarations.
    /// </summary>
    [Fact]
    public void A_downloaded_template_satisfies_its_own_required_columns()
    {
        var bytes = ExcelTemplateWriter.Build("Things", [Code, Quantity]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        workbook.Worksheet("Things").Cell(2, 1).Value = "A1";

        using var filled = new MemoryStream();
        workbook.SaveAs(filled);
        filled.Position = 0;

        var sheet = ExcelSheetReader.Read(filled);

        sheet.IsSuccess.ShouldBeTrue();
        ImportSheetBinder.RequireColumns(sheet.Value, [Code, Quantity]).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_file_with_headers_but_no_rows_is_rejected()
    {
        var result = ExcelSheetReader.Read(Workbook(["Code"], []));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("import.file.no-rows");
    }

    [Theory]
    [InlineData("data.xls", "import.file.extension")]
    [InlineData("data.csv", "import.file.extension")]
    [InlineData("", "import.file.missing")]
    public void Only_xlsx_uploads_are_accepted(string fileName, string expectedCode)
    {
        var result = ExcelUpload.Validate(fileName, length: 100);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(expectedCode);
    }

    private static ExcelSheet Read(string[] headers, string[][] rows) =>
        ExcelSheetReader.Read(Workbook(headers, rows)).Value;

    private static MemoryStream Workbook(string[] headers, string[][] rows)
    {
        var stream = new MemoryStream();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.AddWorksheet("Sheet1");

            for (var column = 0; column < headers.Length; column++)
            {
                worksheet.Cell(1, column + 1).Value = headers[column];
            }

            for (var row = 0; row < rows.Length; row++)
            {
                for (var column = 0; column < rows[row].Length; column++)
                {
                    // Text, so a code like "00123" keeps its leading zeros and the
                    // reader is tested against what a real sheet holds.
                    worksheet.Cell(row + 2, column + 1).SetValue(rows[row][column]);
                }
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }
}

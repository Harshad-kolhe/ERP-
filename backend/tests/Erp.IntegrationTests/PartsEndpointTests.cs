using System.Net;
using System.Net.Http.Json;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;

namespace Erp.IntegrationTests;

/// <summary>
/// The Phase 0 vertical slice, exercised over HTTP against a real database.
/// </summary>
[Collection(nameof(ErpApiCollection))]
public sealed class PartsEndpointTests(ErpApiFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Creating_a_part_then_reading_it_round_trips()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var id = await CreatePartAsync(client, "MTR-1000", "Drive motor 3kW");

        var part = await client.GetFromJsonAsync<PartDetailDto>(
            $"/api/v1/masters/parts/{id}", JsonOptions.Default);

        part.ShouldNotBeNull();
        part.PartNumber.ShouldBe("MTR-1000");
        part.Status.ShouldBe(PartStatusDto.Draft);
        part.BusinessUnitId.ShouldBe(TestUsers.BusinessUnitOne);
    }

    /// <summary>
    /// The audit interceptor runs because the entity implements <c>IAuditable</c>,
    /// not because any handler remembered to stamp it. No handler in this codebase
    /// sets CreatedAtUtc.
    /// </summary>
    [Fact]
    public async Task Audit_columns_are_stamped_without_any_handler_setting_them()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var id = await CreatePartAsync(client, "MTR-1001", "Gearbox");

        var part = await client.GetFromJsonAsync<PartDetailDto>(
            $"/api/v1/masters/parts/{id}", JsonOptions.Default);

        part!.CreatedAtUtc.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        part.ModifiedAtUtc.ShouldBeNull();
    }

    /// <summary>
    /// The single most important behavioural test in the suite. 30 rows exist; the
    /// response carries 10. In the system this replaces, roughly 149 of 180 grids
    /// would have returned all 30 — and all 300,000 on a real table.
    /// </summary>
    [Fact]
    public async Task List_returns_one_page_not_the_whole_table()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        for (var i = 0; i < 30; i++)
        {
            await CreatePartAsync(client, $"PG-{i:D4}", $"Paged part {i}");
        }

        var page = await client.GetFromJsonAsync<PagedResult<PartListItemDto>>(
            "/api/v1/masters/parts?page=1&pageSize=10", JsonOptions.Default);

        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(10);
        page.TotalCount.ShouldBe(30);
        page.TotalPages.ShouldBe(3);
        page.HasNextPage.ShouldBeTrue();
        page.HasPreviousPage.ShouldBeFalse();
    }

    /// <summary>An oversized request is clamped server-side rather than honoured.</summary>
    [Fact]
    public async Task Page_size_is_clamped_to_the_server_maximum()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var page = await client.GetFromJsonAsync<PagedResult<PartListItemDto>>(
            "/api/v1/masters/parts?pageSize=100000", JsonOptions.Default);

        page!.PageSize.ShouldBe(PageRequest.MaxPageSize);
    }

    [Fact]
    public async Task Sorting_and_paging_are_stable_across_pages()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        for (var i = 0; i < 15; i++)
        {
            await CreatePartAsync(client, $"ST-{i:D4}", "Stable ordering");
        }

        var first = await client.GetFromJsonAsync<PagedResult<PartListItemDto>>(
            "/api/v1/masters/parts?page=1&pageSize=5&sort=partNumber:asc", JsonOptions.Default);
        var second = await client.GetFromJsonAsync<PagedResult<PartListItemDto>>(
            "/api/v1/masters/parts?page=2&pageSize=5&sort=partNumber:asc", JsonOptions.Default);

        // No row may appear on two pages: that is what the mandatory tie-breaker buys.
        var firstIds = first!.Items.Select(item => item.Id).ToHashSet();
        second!.Items.ShouldAllBe(item => !firstIds.Contains(item.Id));
    }

    /// <summary>
    /// Proves the <c>QueryMap</c> allow-list. A field the endpoint never declared
    /// is rejected, not concatenated into an ORDER BY clause.
    /// </summary>
    [Fact]
    public async Task Sorting_on_an_undeclared_field_is_rejected()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var response = await client.GetAsync(
            new Uri("/api/v1/masters/parts?sort=passwordHash:asc", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Code.ShouldBe("query.sort.unknown_field");
    }

    [Fact]
    public async Task Filtering_on_an_undeclared_field_is_rejected()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var response = await client.GetAsync(
            new Uri("/api/v1/masters/parts?filter=businessUnitId:eq:2", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Free_text_search_matches_part_number_and_description()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        await CreatePartAsync(client, "SRCH-01", "Hydraulic cylinder");
        await CreatePartAsync(client, "OTHER-01", "Drive belt");

        var page = await client.GetFromJsonAsync<PagedResult<PartListItemDto>>(
            "/api/v1/masters/parts?search=hydraulic", JsonOptions.Default);

        page!.TotalCount.ShouldBe(1);
        page.Items[0].PartNumber.ShouldBe("SRCH-01");
    }

    /// <summary>The unique filtered index is the real guarantee; this is its error message.</summary>
    [Fact]
    public async Task Duplicate_part_number_is_rejected_with_conflict()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        await CreatePartAsync(client, "DUP-01", "First");

        var response = await PostPartAsync(client, "DUP-01", "Second");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Code.ShouldBe("part.number.duplicate");
    }

    /// <summary>Case and whitespace are normalised, so these are the same part number.</summary>
    [Fact]
    public async Task Part_numbers_differing_only_by_case_are_treated_as_duplicates()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        await CreatePartAsync(client, "CASE-01", "First");

        var response = await PostPartAsync(client, "  case-01  ", "Second");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_input_returns_field_level_validation_errors()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var response = await client.PostAsJsonAsync(
            "/api/v1/masters/parts",
            new CreatePartRequest
            {
                PartNumber = string.Empty,
                Description = "No part number",
                UnitOfMeasureCode = "NOS",
                HsnCode = "12", // must be 4, 6 or 8 digits
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Errors.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("PartNumber");
        problem.Errors.ShouldContainKey("HsnCode");
    }

    /// <summary>
    /// The gap this closes: every coded field passed length and format checks and
    /// then went into the database unread, so a part could be saved measured in a
    /// unit that does not exist.
    /// </summary>
    [Fact]
    public async Task A_code_no_master_recognises_is_rejected()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var response = await client.PostAsJsonAsync(
            "/api/v1/masters/parts",
            new CreatePartRequest
            {
                PartNumber = "MTR-2000",
                Description = "Drive motor 5kW",

                // Shaped like a unit and is not one.
                UnitOfMeasureCode = "XYZ",
                Attributes = new PartAttributesDto { SourceCode = "Outsourced" },
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);

        // Both wrong codes named, not just the first one found — an integration
        // posting a part usually gets several fields wrong at once.
        problem!.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("XYZ");
        problem.Detail.ShouldContain("Outsourced");
    }

    /// <summary>
    /// The other half of the same rule: the option the dropdown actually offers is
    /// accepted. Without this, a check that rejected everything would look correct.
    /// </summary>
    [Fact]
    public async Task A_code_the_master_offers_is_accepted()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var response = await client.PostAsJsonAsync(
            "/api/v1/masters/parts",
            new CreatePartRequest
            {
                PartNumber = "MTR-2001",
                Description = "Drive motor 7kW",
                UnitOfMeasureCode = "NOS",
                HsnCode = "85015210",
                Attributes = new PartAttributesDto
                {
                    SourceCode = "OutSource",
                    Moc = "Mild steel",
                    PurchaseUomCode = "KG",
                },
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task<Guid> CreatePartAsync(HttpClient client, string partNumber, string description)
    {
        var response = await PostPartAsync(client, partNumber, description);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreatedPart>(JsonOptions.Default);
        return created!.Id;
    }

    private static Task<HttpResponseMessage> PostPartAsync(HttpClient client, string partNumber, string description) =>
        client.PostAsJsonAsync(
            "/api/v1/masters/parts",
            new CreatePartRequest
            {
                PartNumber = partNumber,
                Description = description,
                UnitOfMeasureCode = "NOS",
            });

    private sealed record CreatedPart(Guid Id);
}

/// <summary>RFC 9457 body, as the tests read it.</summary>
public sealed record ProblemResponse
{
    public string? Type { get; init; }

    public string? Title { get; init; }

    public string? Detail { get; init; }

    public int Status { get; init; }

    public string? Code { get; init; }

    public string? TraceId { get; init; }

    public Dictionary<string, string[]>? Errors { get; init; }
}

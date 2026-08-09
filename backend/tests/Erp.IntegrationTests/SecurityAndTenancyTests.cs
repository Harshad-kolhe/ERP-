using System.Net;
using System.Net.Http.Json;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;

namespace Erp.IntegrationTests;

/// <summary>
/// The behaviours that mattered most in the system being replaced, asserted
/// against a running application and a real database.
/// </summary>
[Collection(nameof(ErpApiCollection))]
public sealed class SecurityAndTenancyTests(ErpApiFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// A body the framework cannot bind is answered 400, not 500.
    /// <para>
    /// Model binding throws before any endpoint filter runs, so <c>ValidationFilter</c>
    /// never sees a payload like this and it lands in the global exception handler.
    /// Answered 500 it would tell an honest client to retry something that can never
    /// succeed, and would file a client's typo among real server faults.
    /// </para>
    /// <para>
    /// Checked on <c>/auth/login</c> deliberately: it is the one endpoint an
    /// unauthenticated caller can reach, so it is where a leaked internal type name
    /// would matter most. The response must carry neither the CLR type nor the
    /// property that was missing.
    /// </para>
    /// </summary>
    [Fact]
    public async Task A_body_the_framework_cannot_bind_is_a_client_error()
    {
        var client = factory.CreateClient();

        // Well-formed JSON, but 'email' is required and absent.
        var response = await client.PostAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new StringContent(
                """{"password":"whatever"}""",
                System.Text.Encoding.UTF8,
                "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain("Erp.Contracts", Case.Insensitive);
        body.ShouldNotContain("System.Text.Json", Case.Insensitive);
        body.ShouldNotContain("Exception", Case.Insensitive);
    }

    /// <summary>Outright malformed JSON takes the same path.</summary>
    [Fact]
    public async Task Syntactically_invalid_json_is_a_client_error()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new StringContent("{ this is not json", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// The fallback authorization policy at work. Three legacy controllers were
    /// reachable anonymously because someone omitted an attribute; here an endpoint
    /// that declares nothing still requires a signed-in user.
    /// </summary>
    [Fact]
    public async Task An_unauthenticated_request_is_rejected()
    {
        var client = factory.CreateWebClient();

        var response = await client.GetAsync(new Uri("/api/v1/masters/parts", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Server-side permission enforcement. The legacy system evaluated permissions
    /// only in JavaScript, so this request would have succeeded.
    /// </summary>
    [Fact]
    public async Task A_user_without_the_permission_is_forbidden()
    {
        var reader = await factory.CreateAuthenticatedClientAsync(TestUsers.Reader);

        var response = await reader.PostAsJsonAsync(
            "/api/v1/masters/parts",
            new CreatePartRequest
            {
                PartNumber = "FORBIDDEN-01",
                Description = "Reader has no create permission",
                UnitOfMeasureCode = "NOS",
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Type.ShouldBe(ProblemTypes.Forbidden);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("masters.part.create");
    }

    [Fact]
    public async Task A_reader_can_still_read()
    {
        var reader = await factory.CreateAuthenticatedClientAsync(TestUsers.Reader);

        var response = await reader.GetAsync(new Uri("/api/v1/masters/parts", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// The global query filter, proven end to end. No handler asked for this: the
    /// filter is applied by convention to every entity implementing
    /// <c>IBusinessUnitScoped</c>. The legacy equivalent was an opt-in
    /// <c>.ApplyBu()</c> call that leaked data wherever it was forgotten.
    /// </summary>
    [Fact]
    public async Task A_part_is_invisible_to_another_business_unit()
    {
        var author = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);
        var otherUnit = await factory.CreateAuthenticatedClientAsync(TestUsers.OtherUnit);

        var id = await CreatePartAsync(author, "TENANT-01", "Business unit 1 only");

        var response = await otherUnit.GetAsync(new Uri($"/api/v1/masters/parts/{id}", UriKind.Relative));

        // 404 rather than 403: confirming the id exists would itself leak information.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_list_never_includes_another_business_units_rows()
    {
        var author = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);
        var otherUnit = await factory.CreateAuthenticatedClientAsync(TestUsers.OtherUnit);

        await CreatePartAsync(author, "BU1-01", "Unit one");
        await CreatePartAsync(otherUnit, "BU2-01", "Unit two");

        var seenByUnitTwo = await otherUnit.GetFromJsonAsync<PagedResult<PartListItemDto>>(
            "/api/v1/masters/parts", JsonOptions.Default);

        seenByUnitTwo!.TotalCount.ShouldBe(1);
        seenByUnitTwo.Items[0].PartNumber.ShouldBe("BU2-01");
    }

    /// <summary>
    /// The same part number in two business units is not a duplicate — the unique
    /// index is scoped to the tenant.
    /// </summary>
    [Fact]
    public async Task The_same_part_number_may_exist_in_two_business_units()
    {
        var author = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);
        var otherUnit = await factory.CreateAuthenticatedClientAsync(TestUsers.OtherUnit);

        await CreatePartAsync(author, "SHARED-01", "Unit one");

        var response = await PostPartAsync(otherUnit, "SHARED-01", "Unit two");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>Optimistic concurrency: the second writer is told, not silently discarded.</summary>
    [Fact]
    public async Task A_stale_row_version_is_rejected_with_conflict()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var id = await CreatePartAsync(client, "CONC-01", "Original");

        var original = await client.GetFromJsonAsync<PartDetailDto>(
            $"/api/v1/masters/parts/{id}", JsonOptions.Default);

        // First writer wins.
        var firstUpdate = await client.PutAsJsonAsync(
            $"/api/v1/masters/parts/{id}",
            BuildUpdate("Updated by the first writer", original!.RowVersion));
        firstUpdate.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Second writer is still holding the version from before that update.
        var secondUpdate = await client.PutAsJsonAsync(
            $"/api/v1/masters/parts/{id}",
            BuildUpdate("Updated by the second writer", original.RowVersion));

        secondUpdate.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await secondUpdate.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Code.ShouldBe("part.stale_row_version");

        var final = await client.GetFromJsonAsync<PartDetailDto>(
            $"/api/v1/masters/parts/{id}", JsonOptions.Default);
        final!.Description.ShouldBe("Updated by the first writer");
    }

    [Fact]
    public async Task A_part_cannot_be_edited_while_awaiting_approval()
    {
        var author = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var id = await CreatePartAsync(author, "LOCK-01", "Original");
        (await author.PostAsync(new Uri($"/api/v1/masters/parts/{id}/submit", UriKind.Relative), null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var current = await author.GetFromJsonAsync<PartDetailDto>(
            $"/api/v1/masters/parts/{id}", JsonOptions.Default);

        var response = await author.PutAsJsonAsync(
            $"/api/v1/masters/parts/{id}",
            BuildUpdate("Changed behind the approver", current!.RowVersion));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Code.ShouldBe("part.not_editable_pending_approval");
    }

    /// <summary>Segregation of duties, enforced in the aggregate and proven over HTTP.</summary>
    [Fact]
    public async Task The_author_cannot_approve_their_own_part()
    {
        var author = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);
        var approver = await factory.CreateAuthenticatedClientAsync(TestUsers.Approver);

        var id = await CreatePartAsync(author, "SOD-01", "Segregation of duties");
        await author.PostAsync(new Uri($"/api/v1/masters/parts/{id}/submit", UriKind.Relative), null);

        // The author does not hold the approve permission at all, so they are stopped
        // one layer earlier — at authorization rather than at the domain rule.
        var byAuthor = await author.PostAsync(new Uri($"/api/v1/masters/parts/{id}/approve", UriKind.Relative), null);
        byAuthor.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // A different person with the permission succeeds.
        var byApprover = await approver.PostAsync(new Uri($"/api/v1/masters/parts/{id}/approve", UriKind.Relative), null);
        byApprover.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var approved = await approver.GetFromJsonAsync<PartDetailDto>(
            $"/api/v1/masters/parts/{id}", JsonOptions.Default);
        approved!.Status.ShouldBe(PartStatusDto.Approved);
    }

    [Fact]
    public async Task A_draft_cannot_be_approved_before_it_is_submitted()
    {
        var author = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);
        var approver = await factory.CreateAuthenticatedClientAsync(TestUsers.Approver);

        var id = await CreatePartAsync(author, "SOD-02", "Still a draft");

        var response = await approver.PostAsync(new Uri($"/api/v1/masters/parts/{id}/approve", UriKind.Relative), null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Error responses carry a correlation id and no internal detail. The legacy
    /// system returned <c>ex.Message</c> to the browser in roughly fifty places,
    /// including raw inner-exception text on the login page.
    /// </summary>
    [Fact]
    public async Task Error_responses_carry_a_trace_id_and_no_internal_detail()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var response = await client.GetAsync(
            new Uri($"/api/v1/masters/parts/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("Exception");
        body.ShouldNotContain("StackTrace");
        body.ShouldNotContain("Microsoft.");

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Code.ShouldBe("part.not_found");
        problem.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>Sign-in must not reveal whether a username exists.</summary>
    [Fact]
    public async Task Sign_in_failures_do_not_distinguish_unknown_user_from_wrong_password()
    {
        var client = factory.CreateWebClient();

        var unknownUser = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "nobody@erp.test", password = TestUsers.Password });

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = TestUsers.Author.UserName, password = "Wrong!Passw0rd123" });

        unknownUser.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var first = await unknownUser.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        var second = await wrongPassword.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);

        second!.Detail.ShouldBe(first!.Detail);
    }

    private static UpdatePartRequest BuildUpdate(string description, string rowVersion) => new()
    {
        Description = description,
        UnitOfMeasureCode = "NOS",
        RowVersion = rowVersion,
    };

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

using System.Net;
using System.Net.Http.Json;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;

namespace Erp.IntegrationTests;

/// <summary>
/// The screens that make every other master maintainable, over HTTP.
/// <para>
/// The tests worth writing here are the ones about consequences, not about CRUD.
/// A create endpoint that returns 201 proves nothing on its own; what matters is
/// that an option added here is one a part will then accept, and that the rules
/// which make units and rates trustworthy cannot be edited around.
/// </para>
/// </summary>
[Collection(nameof(ErpApiCollection))]
public sealed class ReferenceDataEndpointTests(ErpApiFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// The whole point of the screen. Before it existed, adding a material of
    /// construction meant a database migration.
    /// </summary>
    [Fact]
    public async Task An_option_added_here_is_one_a_part_will_accept()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Librarian);

        var rejected = await PostPartAsync(client, "REF-1000", moc: "Inconel 625");
        rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var added = await client.PostAsJsonAsync(
            "/api/v1/masters/lookup-values",
            new CreateLookupValueRequest
            {
                Type = "moc",
                Code = "Inconel 625",
                Name = "Inconel 625",
                SortOrder = 50,
            });

        added.StatusCode.ShouldBe(HttpStatusCode.Created);

        var accepted = await PostPartAsync(client, "REF-1001", moc: "Inconel 625");
        accepted.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>
    /// Retiring is not deleting. The option leaves the dropdown, and — because the
    /// code check reads only active rows — stops being accepted on new records.
    /// </summary>
    [Fact]
    public async Task A_retired_option_drops_out_of_the_list_it_belonged_to()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Librarian);

        var created = await client.PostAsJsonAsync(
            "/api/v1/masters/lookup-values",
            new CreateLookupValueRequest { Type = "moc", Code = "Monel 400", Name = "Monel 400" });

        var id = await IdOfAsync(created);

        var detail = await client.GetFromJsonAsync<LookupValueDetailDto>(
            $"/api/v1/masters/lookup-values/{id}", JsonOptions.Default);

        var retired = await client.PutAsJsonAsync(
            $"/api/v1/masters/lookup-values/{id}",
            new UpdateLookupValueRequest
            {
                Name = "Monel 400",
                IsActive = false,
                RowVersion = detail!.RowVersion,
            });

        retired.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var options = await client.GetFromJsonAsync<LookupSetDto>(
            "/api/v1/masters/lookups?types=moc", JsonOptions.Default);

        options!.Lookups["moc"].ShouldNotContain(option => option.Code == "Monel 400");
    }

    /// <summary>
    /// Units come from their own table now, so the endpoint every form fills its
    /// dropdowns from has to answer for the promoted master too. If it did not, a
    /// unit created here would be invisible on every part form.
    /// </summary>
    [Fact]
    public async Task A_new_unit_appears_in_the_list_forms_read()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Librarian);

        var created = await client.PostAsJsonAsync(
            "/api/v1/masters/units-of-measure",
            new CreateUnitOfMeasureRequest { Code = "GRM", Name = "Gram", Decimals = 3, SortOrder = 40 });

        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        var options = await client.GetFromJsonAsync<LookupSetDto>(
            "/api/v1/masters/lookups?types=uom", JsonOptions.Default);

        options!.Lookups["uom"].ShouldContain(option => option.Code == "GRM");
    }

    /// <summary>
    /// Conversion is one level, not a chain — <c>UnitOfMeasure.BaseCode</c> reads
    /// the base without following it. A unit pointing at a unit that itself
    /// converts would report the wrong family and multiply by the wrong factor, so
    /// the write path refuses to create one.
    /// </summary>
    [Fact]
    public async Task A_unit_cannot_convert_to_a_unit_that_itself_converts()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Librarian);

        // TON already converts to KG in the seeded data.
        var response = await client.PostAsJsonAsync(
            "/api/v1/masters/units-of-measure",
            new CreateUnitOfMeasureRequest
            {
                Code = "KTON",
                Name = "Kilotonne",
                BaseUnitCode = "TON",
                ConversionToBase = 1000m,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Code.ShouldBe("uom.base.not-a-base");
    }

    [Fact]
    public async Task A_unit_with_a_base_needs_a_conversion_factor()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Librarian);

        var response = await client.PostAsJsonAsync(
            "/api/v1/masters/units-of-measure",
            new CreateUnitOfMeasureRequest { Code = "QTL", Name = "Quintal", BaseUnitCode = "KG" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions.Default);
        problem!.Code.ShouldBe("uom.conversion.required");
    }

    /// <summary>
    /// A rate change is an append. The old rate stays exactly as it was, which is
    /// the only reason an invoice raised under it can still be explained.
    /// </summary>
    [Fact]
    public async Task Recording_a_rate_change_keeps_the_rate_it_supersedes()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Librarian);

        var created = await client.PostAsJsonAsync(
            "/api/v1/masters/hsn-codes",
            new CreateHsnCodeRequest
            {
                Code = "84136010",
                Description = "Gear pumps",
                RatePercent = 28m,
                EffectiveFrom = new DateOnly(2017, 7, 1),
            });

        var id = await IdOfAsync(created);

        var amended = await client.PostAsJsonAsync(
            $"/api/v1/masters/hsn-codes/{id}/rates",
            new AddHsnGstRateRequest { RatePercent = 18m, EffectiveFrom = new DateOnly(2025, 9, 22) });

        amended.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var detail = await client.GetFromJsonAsync<HsnCodeDetailDto>(
            $"/api/v1/masters/hsn-codes/{id}", JsonOptions.Default);

        detail!.Rates.Count.ShouldBe(2);
        detail.Rates[0].RatePercent.ShouldBe(18m);
        detail.Rates[1].RatePercent.ShouldBe(28m);
    }

    [Fact]
    public async Task Two_rates_cannot_start_on_the_same_day()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Librarian);

        var created = await client.PostAsJsonAsync(
            "/api/v1/masters/hsn-codes",
            new CreateHsnCodeRequest
            {
                Code = "84139190",
                Description = "Pump parts",
                RatePercent = 18m,
                EffectiveFrom = new DateOnly(2017, 7, 1),
            });

        var id = await IdOfAsync(created);

        var clash = await client.PostAsJsonAsync(
            $"/api/v1/masters/hsn-codes/{id}/rates",
            new AddHsnGstRateRequest { RatePercent = 12m, EffectiveFrom = new DateOnly(2017, 7, 1) });

        clash.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Reading a dropdown needs no permission — every form has to fill one. Changing
    /// what the dropdowns offer is a different power, and this is the test that says
    /// the two are not the same grant.
    /// </summary>
    [Fact]
    public async Task Filling_a_dropdown_is_allowed_where_editing_the_list_is_not()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Author);

        var read = await client.GetAsync(new Uri("/api/v1/masters/lookups?types=moc", UriKind.Relative));
        read.StatusCode.ShouldBe(HttpStatusCode.OK);

        var write = await client.PostAsJsonAsync(
            "/api/v1/masters/lookup-values",
            new CreateLookupValueRequest { Type = "moc", Code = "Titanium", Name = "Titanium" });

        write.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static Task<HttpResponseMessage> PostPartAsync(HttpClient client, string partNumber, string moc) =>
        client.PostAsJsonAsync(
            "/api/v1/masters/parts",
            new CreatePartRequest
            {
                PartNumber = partNumber,
                Description = "Reference data probe",
                UnitOfMeasureCode = "NOS",
                Attributes = new PartAttributesDto { Moc = moc },
            });

    private static async Task<int> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreatedReferenceRecord>(JsonOptions.Default);
        return created!.Id;
    }

    private sealed record CreatedReferenceRecord(int Id);
}

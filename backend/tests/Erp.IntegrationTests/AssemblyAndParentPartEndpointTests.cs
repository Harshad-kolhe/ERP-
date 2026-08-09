using System.Net;
using System.Net.Http.Json;
using Erp.Contracts.Common;
using Erp.Contracts.Masters;

namespace Erp.IntegrationTests;

/// <summary>
/// The four conditional masters — Section, Assembly, Sub-assembly and Parent part —
/// exercised over HTTP against a real database.
/// <para>
/// These tests exist for the rules the legacy system did not have: that a node's
/// parent must be at the level directly above it, that the three levels are three
/// separate permissions over one table, and that a build's totals are arithmetic
/// the server owns rather than numbers the browser posts.
/// </para>
/// </summary>
[Collection(nameof(ErpApiCollection))]
public sealed class AssemblyAndParentPartEndpointTests(ErpApiFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_section_assembly_and_sub_assembly_form_one_chain()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var section = await CreateNodeAsync(client, "sections", "S1", "Frame");
        var assembly = await CreateNodeAsync(client, "assemblies", "A1", "Gearbox", section);
        var subAssembly = await CreateNodeAsync(client, "sub-assemblies", "SA1", "Input shaft", assembly);

        var read = await client.GetFromJsonAsync<AssemblyNodeDetailDto>(
            $"/api/v1/masters/sub-assemblies/{subAssembly}", JsonOptions.Default);

        read.ShouldNotBeNull();
        read.Level.ShouldBe(AssemblyLevelDto.SubAssembly);
        read.ParentId.ShouldBe(assembly);

        // The parent's code and name come back with the id, so the edit screen's
        // picker labels itself without a second request.
        read.ParentCode.ShouldBe("A1");
        read.ParentName.ShouldBe("Gearbox");
    }

    /// <summary>
    /// The rule the legacy system got wrong: it checked that the parent existed but
    /// not what level it was, so a sub-assembly could be filed under another
    /// sub-assembly.
    /// </summary>
    [Fact]
    public async Task A_sub_assembly_cannot_be_filed_under_a_section()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var section = await CreateNodeAsync(client, "sections", "S1", "Frame");

        var response = await PostNodeAsync(client, "sub-assemblies", "SA1", "Input shaft", section);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("assembly.parent.wrong_level");
    }

    [Fact]
    public async Task A_section_cannot_be_given_a_parent()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var section = await CreateNodeAsync(client, "sections", "S1", "Frame");

        var response = await PostNodeAsync(client, "sections", "S2", "Guarding", section);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("assembly.parent.not_allowed");
    }

    [Fact]
    public async Task An_assembly_must_name_a_section()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var response = await PostNodeAsync(client, "assemblies", "A1", "Gearbox", parentId: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("assembly.parent.required");
    }

    /// <summary>
    /// Codes are unique across all three levels, not within one — the code is what
    /// drawings carry, and an <c>S1</c> section beside an <c>S1</c> sub-assembly is
    /// an ambiguity no downstream document can resolve.
    /// </summary>
    [Fact]
    public async Task A_code_used_by_a_section_cannot_be_reused_by_a_sub_assembly()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var section = await CreateNodeAsync(client, "sections", "X1", "Frame");
        var assembly = await CreateNodeAsync(client, "assemblies", "A1", "Gearbox", section);

        var response = await PostNodeAsync(client, "sub-assemblies", "X1", "Input shaft", assembly);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>The whole reason the three levels are three permissions over one table.</summary>
    [Fact]
    public async Task Section_rights_do_not_confer_assembly_rights()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.SectionOnly);

        (await client.GetAsync(new Uri("/api/v1/masters/sections", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        (await client.GetAsync(new Uri("/api/v1/masters/assemblies", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Asking the wrong route for a node is a 404, not a redirect to the right one:
    /// otherwise <c>/sections/{id}</c> would serve sub-assemblies to someone holding
    /// only the section permission.
    /// </summary>
    [Fact]
    public async Task A_node_is_not_readable_through_another_levels_route()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var section = await CreateNodeAsync(client, "sections", "S1", "Frame");

        var response = await client.GetAsync(
            new Uri($"/api/v1/masters/assemblies/{section}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_section_with_active_children_cannot_be_withdrawn()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var section = await CreateNodeAsync(client, "sections", "S1", "Frame");
        await CreateNodeAsync(client, "assemblies", "A1", "Gearbox", section);

        var detail = await client.GetFromJsonAsync<AssemblyNodeDetailDto>(
            $"/api/v1/masters/sections/{section}", JsonOptions.Default);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/masters/sections/{section}",
            new UpdateAssemblyNodeRequest
            {
                Name = "Frame",
                ParentId = null,
                IsActive = false,
                RowVersion = detail!.RowVersion,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).ShouldContain("assembly.has_active_children");
    }

    [Fact]
    public async Task The_section_list_shows_how_many_assemblies_hang_off_each_row()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var section = await CreateNodeAsync(client, "sections", "S1", "Frame");
        await CreateNodeAsync(client, "assemblies", "A1", "Gearbox", section);
        await CreateNodeAsync(client, "assemblies", "A2", "Drive", section);

        var page = await client.GetFromJsonAsync<PagedResult<AssemblyNodeListItemDto>>(
            "/api/v1/masters/sections", JsonOptions.Default);

        page!.Items.Single().ChildCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_parent_part_rolls_its_component_lines_up_into_totals()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1000", "Welded frame");
        var boltId = await CreatePartAsync(client, "BLT-0001", "M12 bolt");
        var plateId = await CreatePartAsync(client, "PLT-0001", "Base plate");

        var id = await CreateParentPartAsync(client, parent, [
            Component(boltId, quantity: 4m, unitWeightKg: 0.25m, rate: 12.50m),
            Component(plateId, quantity: 1m, unitWeightKg: 18m, rate: 900m),
        ]);

        var detail = await client.GetFromJsonAsync<ParentPartDetailDto>(
            $"/api/v1/masters/parent-parts/{id}", JsonOptions.Default);

        detail.ShouldNotBeNull();
        detail.Components.Count.ShouldBe(2);

        // 4 × 0.25 + 1 × 18, and 4 × 12.50 + 1 × 900. Computed by the server from
        // the quantities, never taken from the payload.
        detail.TotalWeightKg.ShouldBe(19m);
        detail.TotalAmount.ShouldBe(950m);

        // The part numbers are resolved server-side, so the screen never looks a
        // part up per line.
        detail.PartNumber.ShouldBe("ASM-1000");
        detail.Components[0].PartNumber.ShouldBe("BLT-0001");
    }

    /// <summary>
    /// The legacy screen took the line amount from the browser and then summed that
    /// column into the header. Here the posted figure is ignored.
    /// </summary>
    [Fact]
    public async Task A_client_supplied_amount_is_ignored()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1001", "Frame");
        var boltId = await CreatePartAsync(client, "BLT-0002", "M12 bolt");

        var id = await CreateParentPartAsync(client, parent, [
            new ParentPartComponentDto
            {
                PartId = boltId,
                Quantity = 2m,
                UnitWeightKg = 1m,
                Rate = 10m,

                // A lie, and the only reason a client could send it is to make the
                // totals say something the quantities do not.
                Amount = 999_999m,
                LineWeightKg = 999_999m,
            },
        ]);

        var detail = await client.GetFromJsonAsync<ParentPartDetailDto>(
            $"/api/v1/masters/parent-parts/{id}", JsonOptions.Default);

        detail!.TotalAmount.ShouldBe(20m);
        detail.Components[0].Amount.ShouldBe(20m);
        detail.Components[0].LineWeightKg.ShouldBe(2m);
    }

    [Fact]
    public async Task A_part_cannot_be_a_component_of_itself()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1002", "Frame");

        var response = await PostParentPartAsync(client, parent, [Component(parent, 1m)]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("parent_part.component.is_parent");
    }

    [Fact]
    public async Task The_same_component_cannot_be_listed_twice()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1003", "Frame");
        var boltId = await CreatePartAsync(client, "BLT-0003", "M12 bolt");

        var response = await PostParentPartAsync(
            client,
            parent,
            [Component(boltId, 1m), Component(boltId, 2m)]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("parent_part.component.duplicate");
    }

    [Fact]
    public async Task A_part_may_only_have_one_build()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1004", "Frame");
        var boltId = await CreatePartAsync(client, "BLT-0004", "M12 bolt");

        await CreateParentPartAsync(client, parent, [Component(boltId, 1m)]);

        var second = await PostParentPartAsync(client, parent, [Component(boltId, 1m)]);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Updating_a_build_replaces_its_lines_and_recomputes_the_totals()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1005", "Frame");
        var boltId = await CreatePartAsync(client, "BLT-0005", "M12 bolt");
        var plateId = await CreatePartAsync(client, "PLT-0005", "Base plate");

        var id = await CreateParentPartAsync(client, parent, [
            Component(boltId, quantity: 10m, unitWeightKg: 1m, rate: 5m),
        ]);

        var before = await client.GetFromJsonAsync<ParentPartDetailDto>(
            $"/api/v1/masters/parent-parts/{id}", JsonOptions.Default);

        before!.TotalAmount.ShouldBe(50m);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/masters/parent-parts/{id}",
            new UpdateParentPartRequest
            {
                Components = [Component(plateId, quantity: 2m, unitWeightKg: 3m, rate: 100m)],
                IsActive = true,
                RowVersion = before.RowVersion,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await client.GetFromJsonAsync<ParentPartDetailDto>(
            $"/api/v1/masters/parent-parts/{id}", JsonOptions.Default);

        after!.Components.Count.ShouldBe(1);
        after.Components[0].PartNumber.ShouldBe("PLT-0005");
        after.TotalAmount.ShouldBe(200m);
        after.TotalWeightKg.ShouldBe(6m);
    }

    [Fact]
    public async Task A_stale_row_version_is_rejected_rather_than_overwriting_the_lines()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1006", "Frame");
        var boltId = await CreatePartAsync(client, "BLT-0006", "M12 bolt");

        var id = await CreateParentPartAsync(client, parent, [Component(boltId, 1m)]);

        var loaded = await client.GetFromJsonAsync<ParentPartDetailDto>(
            $"/api/v1/masters/parent-parts/{id}", JsonOptions.Default);

        // Somebody else saves first.
        (await client.PutAsJsonAsync(
            $"/api/v1/masters/parent-parts/{id}",
            new UpdateParentPartRequest
            {
                Description = "First writer",
                Components = [Component(boltId, 2m)],
                IsActive = true,
                RowVersion = loaded!.RowVersion,
            })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await client.PutAsJsonAsync(
            $"/api/v1/masters/parent-parts/{id}",
            new UpdateParentPartRequest
            {
                Description = "Second writer",
                Components = [],
                IsActive = true,
                RowVersion = loaded.RowVersion,
            });

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var after = await client.GetFromJsonAsync<ParentPartDetailDto>(
            $"/api/v1/masters/parent-parts/{id}", JsonOptions.Default);

        after!.Description.ShouldBe("First writer");
        after.Components.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_build_naming_a_part_that_does_not_exist_is_rejected()
    {
        var client = await factory.CreateAuthenticatedClientAsync(TestUsers.Engineer);

        var parent = await CreatePartAsync(client, "ASM-1007", "Frame");

        var response = await PostParentPartAsync(client, parent, [Component(Guid.CreateVersion7(), 1m)]);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("parent_part.component.not_found");
    }

    private static ParentPartComponentDto Component(
        Guid partId,
        decimal quantity,
        decimal? unitWeightKg = null,
        decimal? rate = null) =>
        new()
        {
            PartId = partId,
            Quantity = quantity,
            UnitWeightKg = unitWeightKg,
            Rate = rate,
        };

    private static async Task<Guid> CreateNodeAsync(
        HttpClient client,
        string resource,
        string code,
        string name,
        Guid? parentId = null)
    {
        var response = await PostNodeAsync(client, resource, code, name, parentId);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var created = await response.Content.ReadFromJsonAsync<CreatedRecord>(JsonOptions.Default);
        return created!.Id;
    }

    private static Task<HttpResponseMessage> PostNodeAsync(
        HttpClient client,
        string resource,
        string code,
        string name,
        Guid? parentId) =>
        client.PostAsJsonAsync(
            $"/api/v1/masters/{resource}",
            new CreateAssemblyNodeRequest { Code = code, Name = name, ParentId = parentId });

    private static async Task<Guid> CreateParentPartAsync(
        HttpClient client,
        Guid partId,
        IReadOnlyList<ParentPartComponentDto> components)
    {
        var response = await PostParentPartAsync(client, partId, components);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var created = await response.Content.ReadFromJsonAsync<CreatedRecord>(JsonOptions.Default);
        return created!.Id;
    }

    private static Task<HttpResponseMessage> PostParentPartAsync(
        HttpClient client,
        Guid partId,
        IReadOnlyList<ParentPartComponentDto> components) =>
        client.PostAsJsonAsync(
            "/api/v1/masters/parent-parts",
            new CreateParentPartRequest { PartId = partId, Components = components });

    private static async Task<Guid> CreatePartAsync(HttpClient client, string partNumber, string description)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/masters/parts",
            new CreatePartRequest
            {
                PartNumber = partNumber,
                Description = description,
                UnitOfMeasureCode = "NOS",
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var created = await response.Content.ReadFromJsonAsync<CreatedRecord>(JsonOptions.Default);
        return created!.Id;
    }

    private sealed record CreatedRecord(Guid Id);
}

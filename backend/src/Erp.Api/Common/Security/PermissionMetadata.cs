namespace Erp.Api.Common.Security;

/// <summary>
/// Endpoint metadata naming the permission an endpoint requires.
/// <para>
/// This is what makes authorization inspectable. <c>EndpointConventionTests</c>
/// enumerates the application's real <c>EndpointDataSource</c> and fails the build
/// if any endpoint lacks this metadata, so "forgot to add the permission check" is
/// a red test rather than a production incident.
/// </para>
/// <para>
/// The legacy system evaluated permissions in JavaScript and performed no
/// server-side role or policy check anywhere, which meant every restriction in the
/// application could be removed with the browser's developer tools.
/// </para>
/// </summary>
/// <param name="Permission">Permission code, e.g. <c>masters.part.create</c>.</param>
public sealed record PermissionMetadata(string Permission) : IPermissionDeclaration;

namespace Erp.BuildingBlocks.Web.Security;

/// <summary>
/// Marks an endpoint that requires authentication but no particular permission.
/// <para>
/// Exists purely so the absence of <see cref="PermissionMetadata"/> is a deliberate,
/// reviewable statement rather than an omission. <c>EndpointConventionTests</c>
/// accepts an endpoint only if it carries a permission, this marker, or
/// <c>AllowAnonymous</c>.
/// </para>
/// </summary>
public sealed record AuthenticatedOnlyMetadata;

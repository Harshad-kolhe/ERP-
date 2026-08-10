namespace Erp.Api.Common.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireAuthenticatedUserAttribute : Attribute, IAuthenticatedOnlyDeclaration;

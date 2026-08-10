using Erp.Contracts.Common;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Erp.Api.Common.Validation;

/// <summary>
/// Runs the registered FluentValidation validators for a request body before the
/// handler sees it.
/// <para>
/// FluentValidation is the single server-side authority. The legacy system spread
/// validation across 168 sparse data annotations, ad-hoc JavaScript per screen, and
/// stored procedures that were not in source control â€” and checked
/// <c>ModelState.IsValid</c> in only 39 places across 61 controllers, so most POST
/// endpoints persisted whatever arrived.
/// </para>
/// <para>
/// Validators are injected as <see cref="IEnumerable{T}"/> so an endpoint with no
/// validator resolves to an empty sequence instead of a DI failure â€” and so this
/// filter never needs to reach into the service provider itself.
/// </para>
/// </summary>
internal sealed class ValidationFilter<TRequest>(IEnumerable<IValidator<TRequest>> validators) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var subject = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (subject is null)
        {
            return await next(context);
        }

        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(subject, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count == 0)
        {
            return await next(context);
        }

        var errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return TypedResults.ValidationProblem(
            errors,
            title: "Invalid request",
            type: ProblemTypes.Validation);
    }
}

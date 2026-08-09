using System.Linq.Expressions;

namespace Erp.BuildingBlocks.Application.Querying;

/// <summary>
/// Combines predicate expressions that were built from different lambdas.
/// Each source lambda has its own parameter instance, so the bodies must be
/// rebound to a single shared parameter before they can be joined — otherwise
/// the resulting tree references a parameter that is not in scope.
/// </summary>
internal static class PredicateCombiner
{
    public static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var body = Expression.OrElse(
            Rebind(left.Body, left.Parameters[0], parameter),
            Rebind(right.Body, right.Parameters[0], parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression Rebind(Expression body, ParameterExpression from, ParameterExpression to) =>
        new ParameterRebinder(from, to).Visit(body);

    private sealed class ParameterRebinder(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}

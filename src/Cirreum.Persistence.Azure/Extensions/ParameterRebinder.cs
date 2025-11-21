namespace Cirreum.Persistence.Extensions;

using System.Collections.Generic;
using System.Linq.Expressions;

internal class ParameterRebinder : ExpressionVisitor {

	readonly Dictionary<ParameterExpression, ParameterExpression> _map;

	internal ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map) =>
		this._map = map ?? [];

	internal static Expression ReplaceParameters(
		Dictionary<ParameterExpression, ParameterExpression> map, Expression exp) =>
		new ParameterRebinder(map).Visit(exp);

	protected override Expression VisitParameter(ParameterExpression parameter) {

		if (this._map.TryGetValue(parameter, out var replacement)) {
			parameter = replacement;
		}

		return base.VisitParameter(parameter);

	}

}
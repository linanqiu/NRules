using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NRules.RuleModel;

namespace NRules.Aggregators
{
    /// <summary>
    /// Aggregator factory for group by aggregator.
    /// </summary>
    internal class GroupByAggregatorFactory : IAggregatorFactory
    {
        private Func<IAggregator> _factory;

        public void Compile(AggregateElement element, IEnumerable<IAggregateExpression> compiledExpressions)
        {
            var keySelector = element.ExpressionCollection["KeySelector"];
            var elementSelector = element.ExpressionCollection["ElementSelector"];

            var sourceType = element.Source.ValueType;
            var keyType = keySelector.Expression.ReturnType;
            var elementType = elementSelector.Expression.ReturnType;
            Type aggregatorType = typeof(GroupByAggregator<,,>).MakeGenericType(sourceType, keyType, elementType);

            var compiledKeySelector = compiledExpressions.FindSingle("KeySelector");
            var compiledElementSelector = compiledExpressions.FindSingle("ElementSelector");
            var ctor = aggregatorType.GetTypeInfo().DeclaredConstructors.Single();
            var factoryExpression = Expression.Lambda<Func<IAggregator>>(
                Expression.New(ctor, Expression.Constant(compiledKeySelector), Expression.Constant(compiledElementSelector)));
            _factory = factoryExpression.Compile();
        }

        public IAggregator Create()
        {
            return _factory();
        }
    }
}
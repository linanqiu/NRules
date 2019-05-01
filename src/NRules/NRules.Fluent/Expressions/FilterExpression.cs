using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NRules.Fluent.Dsl;
using NRules.RuleModel;
using NRules.RuleModel.Builders;

namespace NRules.Fluent.Expressions
{
    internal class FilterExpression : IFilterExpression
    {
        private readonly FilterGroupBuilder _builder;
        private readonly SymbolStack _symbolStack;

        public FilterExpression(FilterGroupBuilder builder, SymbolStack symbolStack)
        {
            _builder = builder;
            _symbolStack = symbolStack;
        }

        public IFilterExpression OnChange(params Expression<Func<object>>[] keySelectors)
        {
            foreach (var keySelector in keySelectors)
            {
                var expression = keySelector.DslExpression(_symbolStack.Scope.Declarations);
                _builder.Filter(FilterType.KeyChange, expression);
            }

            return this;
        }

        public IFilterExpression OnChange<T>(Expression<Func<T>> keySelector, Expression<Func<IEqualityComparer<T>>> comparer) where T : class
        {
            var keySelectorInternalized = InternalizeComparer(keySelector, comparer);
            return OnChange(keySelectorInternalized);
        }

        public IFilterExpression Where(params Expression<Func<bool>>[] predicates)
        {
            foreach (var predicate in predicates)
            {
                var expression = predicate.DslExpression(_symbolStack.Scope.Declarations);
                _builder.Filter(FilterType.Predicate, expression);
            }

            return this;
        }

        private static Expression<Func<object>> InternalizeComparer<T>(Expression<Func<T>> keySelector, Expression<Func<IEqualityComparer<T>>> comparer) where T : class
        {
            var keyExpr = Expression.Invoke(keySelector);
            var compExpr = Expression.Invoke(comparer);
            var ctorInfo = typeof(EqualityWrapper<T>).GetTypeInfo().DeclaredConstructors.First();
            return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(ctorInfo, keyExpr, compExpr), typeof(object)));
        }
    }

    internal class EqualityWrapper<T> : IEquatable<EqualityWrapper<T>>
    {
        public EqualityWrapper(T value, IEqualityComparer<T> comparer)
        {
            Value = value;
            Comparer = comparer;
        }

        public T Value { get; }
        public IEqualityComparer<T> Comparer { get; }

        public bool Equals(EqualityWrapper<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Comparer.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EqualityWrapper<T>) obj);
        }

        public override int GetHashCode() { return Comparer.GetHashCode(Value); }
    }
}
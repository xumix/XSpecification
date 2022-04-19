using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Core;

namespace XSpecification.Linq
{
    public abstract class SpecificationBase<TModel, TFilter>
        where TFilter : class, new()
        where TModel : class, new()
    {
        internal static readonly Expression<Func<TModel, bool>> AlwaysFalseExpression = a => false;

        internal static readonly Expression<Func<TModel, bool>> DoNothing =
            PredicateBuilder.New<TModel>(true).DefaultExpression;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly IDictionary<Type, IDictionary<string, MethodInfo>> TypeMethods =
            new Dictionary<Type, IDictionary<string, MethodInfo>>
            {
                {
                    typeof(Enumerable),
                    new Dictionary<string, MethodInfo>
                    {
                        {
                            nameof(Enumerable.Contains),
                            typeof(Enumerable).GetMethods()
                                              .First(m => m.Name == nameof(Enumerable.Contains) &&
                                                          m.GetParameters().Length == 2)
                        }
                    }
                },
                {
                    typeof(string),
                    new Dictionary<string, MethodInfo>
                    {
                        {
                            nameof(string.Contains),
                            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!
                        },
                        {
                            nameof(string.StartsWith),
                            typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!
                        },
                        {
                            nameof(string.EndsWith),
                            typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!
                        },
                    }
                },
            };

        private static readonly PropertyInfo[] FilterProperties = typeof(TFilter).GetProperties();
        private static readonly PropertyInfo[] ModelProperties = typeof(TModel).GetProperties();

        private readonly ILogger<SpecificationBase<TModel, TFilter>> logger;
        private readonly IOptions<Options> options;

        protected SpecificationBase(
            ILogger<SpecificationBase<TModel, TFilter>> logger,
            IOptions<Options> options)
        {
            this.logger = logger;
            this.options = options;

            var filterProps = FilterProperties;
            var entityProps = ModelProperties.Select(s => s.Name).ToArray();
            var unmatched = filterProps.Select(s => s.Name).Where(f => !entityProps.Contains(f));

            UnmatchedProps = new List<string>(unmatched);
            ExplicitHandlers = new Dictionary<string, FilterPropertyHandler>();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected IList<string> UnmatchedProps { get; set; }

        protected IDictionary<string, FilterPropertyHandler> ExplicitHandlers { get; set; }

        public virtual Expression<Func<TModel, bool>> CreateFilterExpression(TFilter filter)
        {
            var res = PredicateBuilder.New<TModel>(true);

            if (!options.Value.DisableAutoPropertyHandling)
            {
                foreach (var filterProp in FilterProperties)
                {
                    var sourceValue = filterProp.GetValue(filter);

                    if (ExplicitHandlers.ContainsKey(filterProp.Name))
                    {
                        var handler = ExplicitHandlers[filterProp.Name](filterProp, filter);
                        if (handler != DoNothing)
                        {
                            res = res.And(handler);
                        }

                        continue;
                    }

                    var indexProp = typeof(TModel).GetProperty(filterProp.Name);

                    var defHandler = CreateExpressionFromFilterProperty(filterProp, indexProp, sourceValue);

                    // Do not spoil the result with no-ops
                    if (defHandler != DoNothing)
                    {
                        res = res.And(defHandler);
                    }
                }
            }

            if (UnmatchedProps?.Any() == true)
            {
                throw new InvalidOperationException(
                    $"Filter '{typeof(TFilter)}' properties " +
                    $"'{string.Join(",", UnmatchedProps.Select(s => s.ToString()))}'" +
                    $" are not mapped to entity '{typeof(TModel)}' fields");
            }

            var optimized = ExpressionOptimizer.tryVisit(res);
            return (Expression<Func<TModel, bool>>)optimized;
        }

        protected static Expression<Func<TModel, bool>> GetListExpression<TProperty>(
            Expression<Func<TModel, TProperty>> prop,
            IListFilter listFilter)
        {
            if (!listFilter.HasValue())
            {
                return DoNothing;
            }

            var containsMethod = TypeMethods[typeof(Enumerable)][nameof(Enumerable.Contains)];
            var constant = Expression.Constant(listFilter.Cast<TProperty>().ToArray());

            Expression body =
                Expression.Call(containsMethod.MakeGenericMethod(typeof(TProperty)), constant, prop.Body);

            if (listFilter.IsInverted)
            {
                body = Expression.Not(body);
            }

            var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, prop.Parameters);
            return lam;
        }

        protected static Expression<Func<TModel, bool>> GetStringExpression<TProperty>(
            Expression<Func<TModel, TProperty>> prop,
            StringFilter? filter)
        {
            return GetStringExpression<TModel, TProperty>(prop, filter);
        }

        protected static Expression<Func<TM, bool>> GetStringExpression<TM, TProperty>(
            Expression<Func<TM, TProperty>> prop,
            StringFilter? filter)
        {
            if (filter == null || !filter.HasValue())
            {
                return (Expression<Func<TM, bool>>)(object)DoNothing;
            }

            var method = filter switch
            {
                { Contains: true } => TypeMethods[typeof(string)][nameof(string.Contains)],
                { StartsWith: true } => TypeMethods[typeof(string)][nameof(string.StartsWith)],
                { EndsWith: true } => TypeMethods[typeof(string)][nameof(string.EndsWith)],
                _ => null
            };

            var constant = Expression.Constant(filter);
            var value = Expression.MakeMemberAccess(constant, filter.GetMemberInfo(f => f.Value));
            var memberBody = prop.Body;

            Expression body;
            if (method == null)
            {
                body = filter switch
                {
                    { IsNull: true } => Expression.Equal(memberBody, Expression.Constant(null)),
                    { IsNotNull: true } => Expression.NotEqual(memberBody, Expression.Constant(null)),
                    _ => Expression.Equal(memberBody, value)
                };
            }
            else
            {
                body = Expression.NotEqual(memberBody, Expression.Constant(null)); // null check
                body = Expression.AndAlso(body, Expression.Call(memberBody, method, value));
            }

            if (filter.IsInverted)
            {
                body = Expression.Not(body);
            }

            return (Expression<Func<TM, bool>>)Expression.Lambda(body, prop.Parameters);
        }

        protected static Expression<Func<TModel, bool>> GetNullableExpression<TProperty>(
            Expression<Func<TModel, TProperty>> prop,
            INullableFilter value)
        {
            if (!typeof(TProperty).IsNullable())
            {
                return value.IsNull ? AlwaysFalseExpression : DoNothing;
            }

            var memberBody = prop.Body;
            var body = value switch
            {
                { IsNull: true } => Expression.Equal(memberBody, Expression.Constant(null, typeof(TProperty))),
                { IsNotNull: true } => Expression.NotEqual(memberBody, Expression.Constant(null, typeof(TProperty))),
                _ => null
            };

            if (body == null)
            {
                return DoNothing;
            }

            var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, prop.Parameters);
            return lam;
        }

        protected static Expression<Func<TModel, bool>> GetConstantExpression<TProperty>(
            Expression<Func<TModel, TProperty>> prop,
            object? value)
        {
            var body = Expression.Equal(prop.Body, Expression.Constant(value, typeof(TProperty)));
            var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, prop.Parameters);
            return lam;
        }

        protected static Expression<Func<TModel, bool>> GetRangeExpression<TProperty>(
            Expression<Func<TModel, TProperty>> prop,
            IRangeFilter rangeFilter)
        {
            if (rangeFilter.UseStartAsEquals)
            {
                return GetConstantExpression(prop, rangeFilter.Start);
            }

            var param = Expression.Parameter(typeof(TModel));
            var memberBody = new ParameterVisitor(prop.Parameters, new[] { param }).Visit(prop.Body);

            var constant = Expression.Constant(rangeFilter);
            var start = Expression.Property(constant, nameof(IRangeFilter.Start));
            var end = Expression.Property(constant, nameof(IRangeFilter.End));

            if (!memberBody.Type.IsNullable()
                && start.Type.IsNullable())
            {
                memberBody = Expression.Convert(memberBody, start.Type);
            }

            var more = rangeFilter.IsExclusive
                ? Expression.GreaterThan(memberBody, start)
                : Expression.GreaterThanOrEqual(memberBody, start);

            var less = rangeFilter.IsExclusive
                ? Expression.LessThan(memberBody, end)
                : Expression.LessThanOrEqual(memberBody, end);

            Expression? body = (rangeFilter.Start != null, rangeFilter.End != null) switch
            {
                (true, true) => Expression.AndAlso(more, less),
                (true, _) => more,
                (_, true) => less,
                _ => null
            };

            if (body == null)
            {
                return DoNothing;
            }

            var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, param);
            return lam;
        }

        protected virtual void HandleField<TProp>(
            Expression<Func<TFilter, TProp>> filterProp,
            FilterPropertyHandler handler)
        {
            var member = ReflectionHelper.GetPropertyInfo(filterProp);
            if (!UnmatchedProps.Remove(member.Name))
            {
                throw new InvalidOperationException(
                    $"Unable add filter field handler: {filterProp}, property already handled");
            }

            ExplicitHandlers[member.Name] = handler;
        }

        protected virtual void HandleField<TFilterProp, TModelProp>(
            Expression<Func<TFilter, TFilterProp>> filterProp,
            Expression<Func<TModel, TModelProp>> entityProp)
        {
            var member = ReflectionHelper.GetPropertyInfo(filterProp);
            try
            {
                UnmatchedProps.Remove(member.Name);
            }
            catch (Exception e)
            {
                throw new AggregateException($"Unable add filter field handler: {filterProp}, property already handled",
                    e);
            }

            var valueGetter = filterProp.Compile();
            ExplicitHandlers[member.Name] = (prop, filter) =>
            {
                var value = valueGetter(filter);
                return CreateExpressionFromFilterProperty(prop, entityProp, value);
            };
        }

        protected virtual Expression<Func<TModel, bool>> CreateExpressionFromFilterProperty(
            PropertyInfo filterProp,
            PropertyInfo? entityProp,
            object? sourceValue)
        {
            if (entityProp == null)
            {
                return DoNothing;
            }

            var param = Expression.Parameter(typeof(TModel));
            var body = Expression.MakeMemberAccess(param, entityProp);
            var lam = Expression.Lambda(body, param);

            var ret = ReflectionHelper.CallGenericMethod(this,
                nameof(CreateExpressionFromFilterProperty),
                entityProp.PropertyType,
                new object?[] { filterProp, lam, sourceValue });

            return (Expression<Func<TModel, bool>>)ret!;
        }

        protected virtual Expression<Func<TModel, bool>> CreateExpressionFromFilterProperty<TProperty>(
            PropertyInfo filterProp,
            Expression<Func<TModel, TProperty>> entityProp,
            object? sourceValue)
        {
            if (sourceValue == null || sourceValue as string == string.Empty)
            {
                return DoNothing;
            }

            var filterPropType = filterProp.PropertyType;

            try
            {
                CheckTypeCompatibility<TProperty>(sourceValue, filterPropType);

                if (sourceValue is IEnumerable
                    && !(sourceValue is string)
                    && !(sourceValue is INullableFilter))
                {
                    var valType = filterPropType.GetGenericElementType();
                    filterPropType = valType;
                }

                if (typeof(INullableFilter).IsAssignableFrom(filterPropType))
                {
                    var nullableFilter = (INullableFilter)sourceValue;
                    var ret = GetNullableExpression(entityProp, nullableFilter);
                    if (ret != (Expression<Func<TModel, bool>>)DoNothing)
                    {
                        return ret;
                    }
                }

                if (typeof(IListFilter).IsAssignableFrom(filterPropType))
                {
                    var listFilter = (IListFilter)sourceValue;
                    return GetListExpression(entityProp, listFilter);
                }

                if (typeof(StringFilter).IsAssignableFrom(filterPropType))
                {
                    var stringFilter = (StringFilter)sourceValue;
                    return GetStringExpression<TModel, TProperty>(entityProp, stringFilter);
                }

                if (typeof(IRangeFilter).IsAssignableFrom(filterPropType))
                {
                    var rangeFilter = (IRangeFilter)sourceValue;
                    return GetRangeExpression(entityProp, rangeFilter);
                }

                if (sourceValue is IEnumerable enumerable && !(sourceValue is string))
                {
                    var start = PredicateBuilder.New<TModel>(true);

                    foreach (var val in enumerable)
                    {
                        start = start.Or(CreateExpressionFromFilterProperty(filterProp, entityProp, val));
                    }

                    return start ?? DoNothing;
                }

                return GetConstantExpression(entityProp, sourceValue);
            }
            catch (Exception e)
            {
                var format = "Unable to create filter {2} for property: {0}, model field: {1}";
                logger.LogError(format, filterProp.Name, entityProp.Name, GetType());
                throw new AggregateException(string.Format(format, filterProp.Name, entityProp.Body, GetType().Name),
                    e);
            }
        }

        protected void IgnoreField<TProp>(Expression<Func<TFilter, TProp>> filterProp)
        {
            HandleField(filterProp, (_, _) => DoNothing);
        }

        protected delegate Expression<Func<TModel, bool>> FilterPropertyHandler(
            PropertyInfo prop,
            TFilter filter);

        private static void CheckTypeCompatibility<TProperty>(object sourceValue, Type propType)
        {
            if (!propType.IsInstanceOfType(sourceValue)
                && !(sourceValue is IEnumerable && propType.GetGenericElementType().IsInstanceOfType(sourceValue)))
            {
                throw new InvalidOperationException(string.Format(
                    "Filter property type: '{0}' does not match with source value type: '{1}'",
                    propType.Name,
                    sourceValue.GetType().Name));
            }
        }
    }
}

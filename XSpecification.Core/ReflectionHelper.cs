using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace XSpecification.Core
{
    public static class ReflectionHelper
    {
        public const BindingFlags CommonFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly IDictionary<string, MethodInfo> TypeMethods =
            new Dictionary<string, MethodInfo>
            {
                {
                    nameof(Enumerable.Contains), typeof(Enumerable)
                                                 .GetMethods()
                                                 .First(m => m.Name == nameof(Enumerable.Contains) &&
                                                             m.GetParameters().Length == 2)
                },
                {
                    nameof(Enumerable.Cast), typeof(Enumerable)
                                             .GetMethods()
                                             .First(m => m.Name == nameof(Enumerable.Cast) &&
                                                         m.GetParameters().Length == 1)
                },
                {
                    nameof(Enumerable.ToArray), typeof(Enumerable)
                                             .GetMethods()
                                             .First(m => m.Name == nameof(Enumerable.ToArray) &&
                                                         m.GetParameters().Length == 1)
                }
            };

        public static Type? GetClosedOfOpenGeneric(this Type? toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return toCheck;
                }

                toCheck = toCheck.BaseType;
            }

            return null;
        }

        public static PropertyInfo GetPropertyInfo<TObject, TProperty>(Expression<Func<TObject, TProperty>> func)
        {
            var obj = default(TObject);
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var prop = GetMemberInfo(obj, func!) as PropertyInfo;
            if (prop == null)
            {
                throw new InvalidCastException($"Expression '{func}' is not a property.");
            }

            return prop;
        }

        public static MemberInfo GetMemberInfo<TObject, TMember>(
            this TObject target,
            Expression<Func<TObject, TMember>> func)
        {
            var res = func.Body is UnaryExpression unary ? unary.Operand : func.Body;

            if (res is MemberExpression me)
            {
                return me.Member;
            }

            if (res is MethodCallExpression mc)
            {
                return mc.Method;
            }

            throw new ArgumentException(
                "Невалидное выражение. Поддерживается только ображение к свойству и вызов метода.",
                nameof(func));
        }

        public static bool IsNullable(this Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        public static IList? ToArray(this IEnumerable? enumerable, Type type)
        {
            if (enumerable == null)
            {
                return null;
            }

            var castMethod = TypeMethods[nameof(Enumerable.Cast)];
            var arrayMethod = TypeMethods[nameof(Enumerable.ToArray)];
            var castedValue = castMethod.MakeGenericMethod(type)
                                        .Invoke(null, new object?[] { enumerable });

            return (IList)arrayMethod.MakeGenericMethod(type).Invoke(null, new object?[] { castedValue })!;
        }

        public static object? CallGenericMethod(
            object instance,
            string methodName,
            Type genericMethodArgs,
            params object?[] args)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var mi = GetMethodInfo(instance.GetType(),
                methodName,
                CommonFlags | BindingFlags.Static,
                true,
                args?.Length ?? -1);
            mi = mi.MakeGenericMethod(genericMethodArgs);
            return mi.Invoke(instance, args);
        }

        public static Type GetGenericElementType(this Type type)
        {
            // Short-circuit for Array types
            if (typeof(Array).IsAssignableFrom(type))
            {
                return type.GetElementType()!;
            }

            while (true)
            {
                // Type is IEnumerable<T>
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments().First();
                }

                // Type implements/extends IEnumerable<T>
                var elementType = (from subType in type.GetInterfaces()
                                   let retType = subType.GetGenericElementType()
                                   where retType != subType
                                   select retType).FirstOrDefault();

                if (elementType != null)
                {
                    return elementType;
                }

                if (type.BaseType == null)
                {
                    return type;
                }

                type = type.BaseType;
            }
        }

        private static MethodInfo GetMethodInfo(
            Type type,
            string methodName,
            BindingFlags flags,
            bool generic = false,
            int parametersCount = -1)
        {
            var candidates = type.GetMethods(flags).Where(
                                     a => a.Name == methodName && a.IsGenericMethodDefinition == generic
                                           && (parametersCount == -1 || a.GetParameters().Length == parametersCount))
                                 .ToArray();

            if (candidates.Length == 0)
            {
                throw new MissingMethodException(type.FullName, methodName);
            }

            if (candidates.Length > 1)
            {
                throw new AmbiguousMatchException($"Ambigous methods {methodName} found in type {type.FullName}");
            }

            var pi = candidates.Single();

            return pi;
        }
    }
}

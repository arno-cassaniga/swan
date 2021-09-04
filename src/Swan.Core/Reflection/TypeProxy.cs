﻿using Swan.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Swan.Reflection
{
    /// <summary>
    /// Provides a base class for efficiently exposing details about a given type.
    /// </summary>
    internal sealed class TypeProxy : ITypeProxy
    {
        /// <summary>
        /// Binding flags to retrieve instanc, public and non-public members.
        /// </summary>
        private const BindingFlags PublicAndPrivate = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly object SyncLock = new();
        private static readonly Dictionary<Type, Dictionary<string, IPropertyProxy>> PropertyCache = new(32);

        private readonly Lazy<object[]> TypeAttributesLazy;
        private readonly Lazy<ITypeProxy[]> GenericTypeArgumentsLazy;
        private readonly Lazy<FieldInfo[]> FieldsLazy;
        private readonly Lazy<TryParseMethodInfo> TryParseMethodLazy;
        private readonly Lazy<ToStringMethodInfo> ToStringMethodLazy;
        private readonly Lazy<ConstructorInfo?> DefaultConstructorLazy;
        private readonly Lazy<object?> DefaultLazy;
        private readonly Lazy<Func<object>> CreateInstanceLazy;
        private readonly Lazy<ITypeProxy?> ElementTypeLazy;
        private readonly Lazy<Type[]> InterfacesLazy;
        private readonly Lazy<bool> IsEnumerableLazy;
        private readonly Lazy<bool> IsListLazy;
        private readonly Lazy<ITypeProxy?> GenericDictionaryTypeLazy;
        private readonly Lazy<ITypeProxy?> GenericCollectionTypeLazy;

        /// <summary>
        /// Creates a new instance of the <see cref="TypeProxy"/> class.
        /// </summary>
        /// <param name="proxiedType">The type to create a proxy from.</param>
        public TypeProxy(Type proxiedType)
        {
            if (proxiedType.IsGenericType && !proxiedType.IsConstructedGenericType)
                throw new ArgumentException($"Generic type definitions cannot be proxied.");

            ProxiedType = proxiedType ?? throw new ArgumentNullException(nameof(proxiedType));

            var nullableType = Nullable.GetUnderlyingType(proxiedType);
            IsNullableValueType = nullableType != null;
            UnderlyingType = nullableType ?? ProxiedType;
            IsNumeric = TypeManager.NumericTypes.Contains(UnderlyingType);
            IsBasicType = TypeManager.BasicValueTypes.Contains(UnderlyingType);

            FieldsLazy = new(() => proxiedType.GetFields(PublicAndPrivate), true);
            TypeAttributesLazy = new(() => proxiedType.GetCustomAttributes(true), true);
            TryParseMethodLazy = new(() => new TryParseMethodInfo(this), true);
            ToStringMethodLazy = new(() => new ToStringMethodInfo(this), true);
            DefaultConstructorLazy = new(() => !IsValueType
                ? proxiedType.GetConstructor(PublicAndPrivate, null, Type.EmptyTypes, null)
                : null, true);

            DefaultLazy = new(() => IsValueType ? Activator.CreateInstance(proxiedType) : null, true);
            CreateInstanceLazy = new(() =>
            {
                if (IsValueType)
                    return new(() => DefaultValue);

                var constructor = DefaultConstructorLazy.Value;
                return constructor is not null
                    ? (Func<object>)Expression.Lambda(Expression.New(constructor)).Compile()
                    : () => throw new MissingMethodException($"Type '{ProxiedType.Name}' does not have a parameterless constructor.");

            }, true);
            InterfacesLazy = new(() => ProxiedType.GetInterfaces(), true);
            IsEnumerableLazy = new(() => IsArray || Interfaces.Any(c => c == typeof(IEnumerable)), true);
            IsListLazy = new(() => Interfaces.Any(c => c == typeof(IList)), true);

            ElementTypeLazy = new(() =>
            {
                return IsArray
                    ? (ProxiedType.GetElementType() ?? typeof(object)).TypeInfo()
                    : ProxiedType.HasElementType
                    ? ProxiedType.GetElementType()?.TypeInfo()
                    : null;
            }, true);

            GenericTypeArgumentsLazy = new(() =>
            {
                return ProxiedType.GenericTypeArguments.Select(c => c.TypeInfo()).ToArray();
            }, true);

            GenericCollectionTypeLazy = new(() =>
            {
                var genericInterface = Interfaces.FirstOrDefault(
                    c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(ICollection<>));

                if (genericInterface is null || genericInterface.GenericTypeArguments.Length < 1)
                    return null;

                var elementType = genericInterface.GenericTypeArguments[0];

                return typeof(ICollection<>)
                    .MakeGenericType(elementType)
                    .TypeInfo();
            }, true);

            GenericDictionaryTypeLazy = new(() =>
            {
                var genericInterface = Interfaces.FirstOrDefault(
                    c => c.IsGenericType && c.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                if (genericInterface is null || genericInterface.GenericTypeArguments.Length < 2)
                    return null;

                var keyType = genericInterface.GenericTypeArguments[0];
                var valueType = genericInterface.GenericTypeArguments[1];

                return typeof(IDictionary<,>)
                    .MakeGenericType(keyType, valueType)
                    .TypeInfo();

            }, true);
        }

        /// <inheritdoc />
        public Type ProxiedType { get; }

        /// <inheritdoc />
        public bool IsNullableValueType { get; }

        /// <inheritdoc />
        public bool IsNumeric { get; }

        /// <inheritdoc />
        public bool IsConstructedGenericType => ProxiedType.IsConstructedGenericType;

        /// <inheritdoc />
        public bool IsValueType => ProxiedType.IsValueType;

        /// <inheritdoc />
        public bool IsAbstract => ProxiedType.IsAbstract;

        /// <inheritdoc />
        public bool IsInterface => ProxiedType.IsInterface;

        /// <inheritdoc />
        public bool IsEnum => ProxiedType.IsEnum;

        /// <inheritdoc />
        public bool IsArray => ProxiedType.IsArray;

        /// <inheritdoc />
        public bool IsBasicType { get; }

        /// <inheritdoc />
        public Type UnderlyingType { get; }

        /// <inheritdoc />
        public object? DefaultValue => DefaultLazy.Value;

        /// <inheritdoc />
        public bool CanParseNatively => ProxiedType == typeof(string) || TryParseMethodInfo != null;

        /// <inheritdoc />
        public bool CanCreateInstance => IsValueType || (!IsAbstract && !IsInterface && DefaultConstructorLazy.Value is not null);

        /// <inheritdoc />
        public bool IsEnumerable => IsEnumerableLazy.Value;

        /// <inheritdoc />
        public bool IsList => IsListLazy.Value;

        /// <inheritdoc />
        public IReadOnlyList<ITypeProxy> GenericTypeArguments => GenericTypeArgumentsLazy.Value;

        /// <inheritdoc />
        public ITypeProxy? GenericDictionaryType => GenericDictionaryTypeLazy.Value;

        /// <inheritdoc />
        public ITypeProxy? GenericCollectionType => GenericCollectionTypeLazy.Value;

        public bool HasElementType => ProxiedType.HasElementType;

        /// <inheritdoc />
        public ITypeProxy? ElementType => ElementTypeLazy.Value;

        /// <inheritdoc />
        public IReadOnlyList<Type> Interfaces => InterfacesLazy.Value;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IPropertyProxy> Properties
        {
            get
            {
                lock (SyncLock)
                {
                    if (PropertyCache.TryGetValue(ProxiedType, out var proxies))
                        return proxies;

                    var properties = ProxiedType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    proxies = new Dictionary<string, IPropertyProxy>(properties.Length, StringComparer.InvariantCulture);
                    foreach (var propertyInfo in properties)
                    {
                        // skip indexers
                        if (propertyInfo.GetIndexParameters().Length > 0)
                            continue;

                        proxies[propertyInfo.Name] = new PropertyProxy(ProxiedType, propertyInfo);
                    }


                    PropertyCache.TryAdd(ProxiedType, proxies);
                    return proxies;
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<FieldInfo> Fields => FieldsLazy.Value;

        /// <inheritdoc />
        public IReadOnlyList<object> TypeAttributes => TypeAttributesLazy.Value;

        private MethodInfo? TryParseMethodInfo => TryParseMethodLazy.Value.Method;

        private MethodInfo? ToStringMethodInfo => ToStringMethodLazy.Value.Method;

        /// <inheritdoc />
        public object CreateInstance() => CreateInstanceLazy.Value.Invoke();

        /// <inheritdoc />
        public string ToStringInvariant(object? instance)
        {
            if (instance is null)
                return string.Empty;

            return ToStringMethodInfo is not null && ToStringMethodLazy.Value.Parameters.Count == 1
                ? ToStringMethodInfo.Invoke(instance, new object[] { CultureInfo.InvariantCulture }) as string ?? string.Empty
                : instance.ToString() ?? string.Empty;
        }

        /// <inheritdoc />
        public bool TryParse(string s, out object? result)
        {
            result = DefaultValue;

            try
            {
                if (ProxiedType == typeof(string))
                {
                    result = Convert.ChangeType(s, ProxiedType, CultureInfo.InvariantCulture);
                    return true;
                }

                if ((IsNullableValueType && string.IsNullOrEmpty(s)) || !CanParseNatively)
                {
                    return true;
                }

                // Build the arguments of the TryParse method
                var dynamicArguments = new List<object?> { s };

                for (var pi = 1; pi < TryParseMethodLazy.Value.Parameters.Count - 1; pi++)
                {
                    var argInfo = TryParseMethodLazy.Value.Parameters[pi];
                    if (argInfo.ParameterType == typeof(IFormatProvider))
                        dynamicArguments.Add(CultureInfo.InvariantCulture);
                    else if (argInfo.ParameterType == typeof(NumberStyles))
                        dynamicArguments.Add(NumberStyles.Any);
                    else
                        dynamicArguments.Add(null);
                }

                dynamicArguments.Add(null);
                var parseArguments = dynamicArguments.ToArray();

                if ((bool)(TryParseMethodInfo?.Invoke(null, parseArguments) ?? false))
                {
                    result = parseArguments[^1];
                    return true;
                }
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Type Proxy: {ProxiedType.Name}";
        }
    }
}
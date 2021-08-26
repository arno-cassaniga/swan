﻿using Swan.Reflection;
using System;
using System.Linq;

namespace Swan.Parsers
{
    /// <summary>
    /// Provides methods to parse command line arguments.
    /// </summary>
    public partial class ArgumentParser
    {
        private sealed class TypeResolver<T>
        {
            private readonly string _selectedVerb;

            private IPropertyProxy[]? _properties;

            public TypeResolver(string selectedVerb) => _selectedVerb = selectedVerb;

            public bool HasVerb { get; private set; }

            public IPropertyProxy[]? Properties => _properties?.Any() == true ? _properties : null;

            public object? GetOptionsObject(T instance)
            {
                _properties = typeof(T).TypeInfo().Properties.Values.ToArray();

                if (!_properties.Any(x => x.HasAttribute<VerbOptionAttribute>()))
                    return instance;

                HasVerb = true;

                var selectedVerb = string.IsNullOrWhiteSpace(_selectedVerb)
                    ? null
                    : _properties.FirstOrDefault(x => x.Attribute<VerbOptionAttribute>()?.Name == _selectedVerb);

                if (selectedVerb == null) return null;

                var type = instance.GetType();

                var verbProperty = type.GetProperty(selectedVerb.Name);

                if (verbProperty?.GetValue(instance) == null)
                {
                    var propertyInstance = Activator.CreateInstance(selectedVerb.PropertyType);
                    verbProperty?.SetValue(instance, propertyInstance);
                }

                _properties = selectedVerb.PropertyType.TypeInfo().Properties.Values.ToArray();

                return verbProperty?.GetValue(instance);
            }
        }
    }
}
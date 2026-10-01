#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Internal
{
    static class Snapshots
    {
        internal static string Text(string value, string parameter)
        {
            if (value is null)
                throw new ArgumentNullException(parameter);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty identifier is required.", parameter);
            return value;
        }

        internal static PackageId Id(PackageId value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value.value))
                throw new ArgumentException("An initialized package identifier is required.", parameter);
            return value;
        }

        internal static IReadOnlyList<T> List<T>(IEnumerable<T> values)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            T[] copy = values.ToArray();
            if (copy.Any(x => x is null))
                throw new ArgumentException("Snapshot elements cannot be null.", nameof(values));
            return Array.AsReadOnly(copy);
        }

        internal static IReadOnlyDictionary<TKey, TValue> Map<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values)
            where TKey : notnull
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            Dictionary<TKey, TValue> copy = new();
            foreach (KeyValuePair<TKey, TValue> item in values)
            {
                if (item.Key is null || item.Value is null)
                    throw new ArgumentException("Snapshot entries cannot be null.", nameof(values));
                copy.Add(item.Key, item.Value);
            }
            return new ReadOnlyDictionary<TKey, TValue>(copy);
        }

        internal static bool SameSource(PackageSource left, PackageSource right) =>
            left.kind == right.kind && left.key == right.key;

        internal static bool SameInstallation(PackageInstallation left, PackageInstallation right) =>
            left.kind == right.kind && left.key == right.key;
    }
}

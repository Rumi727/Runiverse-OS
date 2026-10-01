#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;

namespace RuniOS.PackageManagement.Unity.Editor.Internal
{
    internal static class AdapterData
    {
        internal static string Text(string value, string name)
        {
            if (value is null) throw new ArgumentNullException(name);
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", name);
            return value;
        }
        internal static string PackageName(string value)
        {
            Text(value, nameof(value));
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-z0-9][a-z0-9._-]*$"))
                throw new ArgumentException("A lowercase UPM package name is required.", nameof(value));
            return value;
        }
        internal static IReadOnlyList<T> List<T>(IEnumerable<T> values) => Array.AsReadOnly(values.ToArray());
        internal static PackageResult<T> Failure<T>(string code, PackageDiagnosticPhase phase, string message) where T : class =>
            new(null, new[] { PackageDiagnostic.Error(code, phase, message) });
        internal static string HttpUrl(string value)
        {
            Uri uri = new(Text(value, nameof(value)), UriKind.Absolute);
            if ((uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("An HTTP(S) URL without credentials, query or fragment is required.", nameof(value));
            return uri.AbsoluteUri.TrimEnd('/');
        }
    }
}

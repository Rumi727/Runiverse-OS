#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Unity.Editor.Internal.Json
{
    internal static class JsonData
    {
        internal static Dictionary<string, object?> Parse(string text) => Object(new JsonReader(text.TrimStart('\uFEFF')).Read());
        internal static Dictionary<string, object?> Object(object? value) => value as Dictionary<string, object?> ??
            throw new FormatException("Expected a JSON object.");
        internal static List<object?> Array(object? value) => value as List<object?> ?? throw new FormatException("Expected a JSON array.");
        internal static string String(object? value) => value as string ?? throw new FormatException("Expected a JSON string.");
        internal static string Required(Dictionary<string, object?> value, string key) => String(value.TryGetValue(key, out object? entry) ? entry : null);
        internal static string? Optional(Dictionary<string, object?> value, string key) => value.TryGetValue(key, out object? entry) ? String(entry) : null;
        internal static Dictionary<string, object?> Map(Dictionary<string, object?> value, string key) =>
            value.TryGetValue(key, out object? entry) ? Object(entry) : new Dictionary<string, object?>(StringComparer.Ordinal);
        internal static string Canonical(string json) => Write(Parse(json), sortKeys: true);
        internal static string Write(object? value, bool pretty = false, bool sortKeys = false)
        {
            System.Text.StringBuilder output = new();
            void Emit(object? item, int depth)
            {
                void Indent(int level) { if (pretty) output.Append('\n').Append(' ', level * 2); }
                if (item is null) output.Append("null");
                else if (item is string text)
                {
                    output.Append('"');
                    foreach (char character in text)
                    {
                        if (character == '"' || character == '\\') output.Append('\\').Append(character);
                        else if (character < 32 || char.IsSurrogate(character)) output.Append("\\u").Append(((int)character).ToString("x4"));
                        else output.Append(character);
                    }
                    output.Append('"');
                }
                else if (item is bool boolean) output.Append(boolean ? "true" : "false");
                else if (item is JsonNumber number) output.Append(number.text);
                else if (item is IDictionary<string, object?> map)
                {
                    output.Append('{'); bool first = true;
                    foreach (var entry in sortKeys ? map.OrderBy(x => x.Key, StringComparer.Ordinal) : (IEnumerable<KeyValuePair<string, object?>>)map)
                    {
                        if (!first) output.Append(','); first = false; Indent(depth + 1);
                        Emit(entry.Key, depth + 1); output.Append(pretty ? ": " : ":"); Emit(entry.Value, depth + 1);
                    }
                    if (!first) Indent(depth); output.Append('}');
                }
                else if (item is IEnumerable<object?> list)
                {
                    output.Append('['); bool first = true;
                    foreach (object? entry in list)
                    {
                        if (!first) output.Append(','); first = false; Indent(depth + 1); Emit(entry, depth + 1);
                    }
                    if (!first) Indent(depth); output.Append(']');
                }
                else throw new ArgumentException("Unsupported JSON value.", nameof(value));
            }
            Emit(value, 0); return output.ToString();
        }
    }
}

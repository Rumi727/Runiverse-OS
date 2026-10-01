#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Unity.Editor.Internal.Json
{
    internal sealed class JsonReader
    {
        static readonly System.Text.RegularExpressions.Regex numberPattern = new(@"\G-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?");
        readonly string text;
        int position;
        internal JsonReader(string text) => this.text = text ?? throw new ArgumentNullException(nameof(text));
        internal object? Read()
        {
            object? result = Value(0);
            Space();
            if (position != text.Length) throw Error("Unexpected trailing input.");
            return result;
        }
        void Space() { while (position < text.Length && " \t\r\n".IndexOf(text[position]) >= 0) position++; }
        FormatException Error(string message) => new($"{message} JSON offset {position}.");
        bool Take(char value)
        {
            Space();
            if (position < text.Length && text[position] == value) { position++; return true; }
            return false;
        }
        void Expect(char value) { if (!Take(value)) throw Error($"Expected '{value}'."); }
        object? Value(int depth)
        {
            if (depth > 128) throw Error("JSON nesting is too deep.");
            Space();
            if (position >= text.Length) throw Error("Unexpected end of input.");
            char current = text[position];
            if (current == '"') return String();
            if (Take('{'))
            {
                Dictionary<string, object?> result = new(StringComparer.Ordinal);
                if (Take('}')) return result;
                do
                {
                    Space();
                    string key = String(); Expect(':');
                    if (!result.TryAdd(key, Value(depth + 1))) throw Error($"Duplicate JSON key '{key}'.");
                    if (Take('}')) return result;
                    Expect(',');
                } while (true);
            }
            if (Take('['))
            {
                List<object?> result = new();
                if (Take(']')) return result;
                do
                {
                    result.Add(Value(depth + 1));
                    if (Take(']')) return result;
                    Expect(',');
                } while (true);
            }
            foreach (string literal in new[] { "true", "false", "null" })
            {
                if (position + literal.Length <= text.Length && string.CompareOrdinal(text, position, literal, 0, literal.Length) == 0)
                {
                    position += literal.Length;
                    return literal == "null" ? null : (object)(literal == "true");
                }
            }
            var match = numberPattern.Match(text, position);
            if (!match.Success || match.Index != position) throw Error("Expected a JSON value.");
            position += match.Length;
            return new JsonNumber(match.Value);
        }
        string String()
        {
            if (position >= text.Length || text[position++] != '"') throw Error("Expected a string.");
            System.Text.StringBuilder result = new();
            while (position < text.Length)
            {
                char value = text[position++];
                if (value == '"') return result.ToString();
                if (value < 32) throw Error("Unescaped control character.");
                if (value != '\\') { result.Append(value); continue; }
                if (position >= text.Length) throw Error("Incomplete escape.");
                char escape = text[position++];
                switch (escape)
                {
                    case '"': case '\\': case '/': result.Append(escape); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (position + 4 > text.Length || !ushort.TryParse(text.Substring(position, 4),
                            System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ushort unicode))
                            throw Error("Invalid Unicode escape.");
                        result.Append((char)unicode); position += 4; break;
                    default: throw Error("Invalid escape.");
                }
            }
            throw Error("Unterminated string.");
        }
    }
}

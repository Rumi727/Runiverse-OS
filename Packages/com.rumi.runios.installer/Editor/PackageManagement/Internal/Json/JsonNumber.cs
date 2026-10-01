#nullable enable
using System.Linq;

namespace RuniOS.PackageManagement.Unity.Editor.Internal.Json
{
    internal readonly struct JsonNumber
    {
        internal string text { get; }
        internal JsonNumber(string text) => this.text = text;
    }
}

#nullable enable
using UnityEngine;

namespace RuniEngine
{
    public sealed class TooltipAttribute : PropertyAttribute
    {
        public TooltipAttribute(string text) => this.text = text;

        public string text { get; } = "";
    }
}

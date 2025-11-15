#nullable enable
namespace RuniOS.Utility.Attributes;

public sealed class TooltipAttribute : PropertyAttribute
{
    public TooltipAttribute(string text) => this.text = text;

    public string text { get; } = "";
}
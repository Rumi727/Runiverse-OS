#nullable enable
namespace RuniOS.Inspectors
{
    public interface IInspectorListElement : IInspectorVariableElement
    {
        int index { get; set; }
    }
}
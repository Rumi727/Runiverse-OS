#nullable enable
using System.Collections.Immutable;

namespace RuniOS.Inspectors
{
    public interface IInspectable
    {
        string inspectionDisplayName { get; }
        
        ImmutableArray<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.All);
    }
}
using RuniOS.Inspectors.Drawers;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace RuniOS.Inspectors
{
    public interface IInspector
    {
        IInspectable? inspectable { get; }
        ImmutableArray<IInspectorElement> elements { get; }
        
        IEnumerable<InspectorDrawer?> drawers { get; }
        
        InspectorFlags inspectorFlags { get; }

        void Rebuild(IInspectable inspectable, InspectorFlags flags = InspectorFlags.All);
        void Rebuild(IEnumerable<IInspectorElement> elements, InspectorFlags flags = InspectorFlags.All);
    }
}
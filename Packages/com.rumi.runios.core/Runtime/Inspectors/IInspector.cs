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

        void Rebuild(IInspectable inspectable, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);
        
        void Rebuild(IInspectorElement element, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool skipFlagCheck = false);
        void Rebuild(IEnumerable<IInspectorElement> elements, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool skipFlagCheck = false);
    }
}
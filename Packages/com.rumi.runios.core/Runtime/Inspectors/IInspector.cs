using RuniOS.Inspectors.Drawers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Inspectors
{
    public interface IInspector
    {
        [DisallowNull]
        IInspectable? inspectable { get; set; }
        
        ImmutableArray<IInspectorElement> elements { get; }
        IEnumerable<InspectorDrawer?> drawers { get; }
        
        InspectorFlags inspectorFlags { get; set; }
    }
}
using RuniOS.Inspectors.Attributes;
using RuniOS.Undos;
using System.Collections.Immutable;

namespace RuniOS.Inspectors
{
    public interface IInspector
    {
        IInspectable? inspectable { get; }

        IInspectorElement? element { get; }
        ImmutableArray<IInspectorElement> elements { get; }

        InspectorFlags inspectorFlags { get; }
        
        ImmutableArray<IInspectorAttribute> inheritedAttributes { get; }
        
        IUndoRecorder? undoRecorder { get; }

        void Rebuild(IInspectable inspectable, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);

        void Rebuild(IInspectorElement element, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool skipFlagCheck = false);
        void Rebuild(IEnumerable<IInspectorElement> elements, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool skipFlagCheck = false);
    }
}
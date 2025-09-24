#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.Inspectors
{
    public interface IInspectableObject : IInspectable
    {
        IInspectorVariableElement? parentElement { get; }
        
        object? instance => instances.FirstOrDefault();
        IEnumerable<object> instances { get; }
    }
}
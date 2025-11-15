#nullable enable
using System.Reflection;

namespace RuniOS.Inspectors;

public static class InspectorFlagsExtensions
{
    public static bool HasFlagFast(this InspectorFlags value, InspectorFlags flag) => (value & flag) != 0;
        
    public static BindingFlags ToBindingFlags(this InspectorFlags value)
    {
        BindingFlags bindingFlags = BindingFlags.Default;
        if (value.HasFlagFast(InspectorFlags.Public)) bindingFlags |= BindingFlags.Public;
        if (value.HasFlagFast(InspectorFlags.NonPublic)) bindingFlags |= BindingFlags.NonPublic;
        if (value.HasFlagFast(InspectorFlags.Static)) bindingFlags |= BindingFlags.Static;
        if (value.HasFlagFast(InspectorFlags.Instance)) bindingFlags |= BindingFlags.Instance;

        return bindingFlags;
    }
}
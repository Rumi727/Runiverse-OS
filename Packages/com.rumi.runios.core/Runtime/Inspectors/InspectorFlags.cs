#nullable enable
using System;
using System.Reflection;

namespace RuniOS.Inspectors
{
    [Flags]
    public enum InspectorFlags
    {
        None = 0,
        Public = 1 << 0,
        NonPublic = 1 << 1,
        Static = 1 << 2,
        Instance = 1 << 3,
        ReadOnly = 1 << 4,
        WriteOnly = 1 << 5,
        PublicAccess = Public | Static | Instance | ReadOnly | WriteOnly,
        Access = PublicAccess | NonPublic,
        Property = 1 << 10,
        Event = 1 << 11,
        Field = 1 << 12,
        Method = 1 << 13,
        Variable = Property | Event | Field,
        Member = Variable | Method,
        List = 1 << 20,
        All = -1
    }

    public static class InspectorFlagsExtensions
    {
        public static bool HasFlagFast(this InspectorFlags value, InspectorFlags flag) => (value & flag) != 0;
        
        public static BindingFlags ToBindingFlags(this InspectorFlags value)
        {
            BindingFlags bindingFlags = BindingFlags.Default;
            if (value.HasFlagFast(InspectorFlags.Public)) bindingFlags = BindingFlags.Public;
            if (value.HasFlagFast(InspectorFlags.NonPublic)) bindingFlags = BindingFlags.NonPublic;
            if (value.HasFlagFast(InspectorFlags.Static)) bindingFlags = BindingFlags.Static;
            if (value.HasFlagFast(InspectorFlags.Instance)) bindingFlags = BindingFlags.Instance;

            return bindingFlags;
        }
    }
}
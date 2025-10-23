#nullable enable
using System;

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
}
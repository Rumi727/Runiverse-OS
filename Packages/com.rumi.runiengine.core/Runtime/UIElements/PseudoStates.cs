#nullable enable
using System;

namespace RuniEngine.UIElements
{
    [Flags]
    public enum PseudoStates
    {
        Active = 1,
        Hover = 2,
        Checked = 8,
        Disabled = 32,
        Focus = 64,
        Root = 128
    }
}
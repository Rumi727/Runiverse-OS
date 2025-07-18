#nullable enable
using System;
using UnityEngine;

namespace RuniEngine
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class NotNullFieldAttribute : PropertyAttribute
    {

    }
}
#nullable enable
using System;
using UnityEngine;

namespace RuniOS
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class NotNullFieldAttribute : PropertyAttribute
    {

    }
}
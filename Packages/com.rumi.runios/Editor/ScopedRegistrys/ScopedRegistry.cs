using System;
using UnityEngine;

namespace RuniOS.Installer.ScopedRegistrys
{
    //[CreateAssetMenu(fileName = "ScopedRegistry", menuName = "Scriptable Objects/ScopedRegistry")]
    class ScopedRegistry : ScriptableObject
    {
        public ScopedRegistry[] scopedRegistries = Array.Empty<ScopedRegistry>();
        
        public new string? name;
        public string? url;
        
        public string[]? scopes;
    }
}

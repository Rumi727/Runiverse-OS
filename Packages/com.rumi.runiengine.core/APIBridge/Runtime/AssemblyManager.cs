#nullable enable
using System;
using System.Linq;
using System.Reflection;

namespace RuniEngine.APIBridge
{
    internal static class AssemblyManager
    {
        public static Assembly[] assemblys => _assemblys ??= AppDomain.CurrentDomain.GetAssemblies();
        static Assembly[]? _assemblys;

        public static Assembly UnityEngine_CoreModule => _UnityEngine_CoreModule ??= assemblys.First(x => x.GetName().Name == "UnityEngine.CoreModule");
        static Assembly? _UnityEngine_CoreModule;
    }
}
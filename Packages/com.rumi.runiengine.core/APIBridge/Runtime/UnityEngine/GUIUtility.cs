#nullable enable
using System;
using System.Reflection;

namespace RuniEngine.APIBridge.UnityEngine
{
    public class GUIUtility
    {
        public static Type type { get; } = typeof(global::UnityEngine.GUIUtility);

        protected GUIUtility() { }



        public static int s_ControlCount
        {
            get
            {
                f_s_ControlCount ??= type.GetField("s_ControlCount", BindingFlags.NonPublic | BindingFlags.Static);
                return (int)f_s_ControlCount.GetValue(null);
            }
            set
            {
                f_s_ControlCount ??= type.GetField("s_ControlCount", BindingFlags.NonPublic | BindingFlags.Static);
                f_s_ControlCount.SetValue(null, value);
            }
        }
        static FieldInfo? f_s_ControlCount;
    }
}

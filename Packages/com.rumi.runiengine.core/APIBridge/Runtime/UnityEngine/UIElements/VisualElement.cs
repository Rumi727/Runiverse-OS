#nullable enable
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class VisualElement : Focusable
    {
        public static new Type type { get; } = typeof(global::UnityEngine.UIElements.VisualElement);

        public static VisualElement GetInstance(global::UnityEngine.UIElements.VisualElement instance) => new VisualElement(instance);

        protected VisualElement(global::UnityEngine.UIElements.VisualElement instance) : base(instance) => this.instance = instance;

        public new global::UnityEngine.UIElements.VisualElement instance { get; set; }

        

        static MethodInfo? m_IncrementVersion;
        static readonly object[] mp_IncrementVersion = new object[1];
        static readonly Type[] mpt_IncrementVersion = new Type[] { typeof(VersionChangeType) };
        public void IncrementVersion(VersionChangeType changeType)
        {
            m_IncrementVersion ??= type.GetMethod("IncrementVersion", BindingFlags.NonPublic | BindingFlags.Instance, null, mpt_IncrementVersion, null);

            mp_IncrementVersion[0] = changeType;
            m_IncrementVersion.Invoke(instance, mp_IncrementVersion);
        }
    }
}

#nullable enable
using System;
using System.Reflection;
using UnityEngine;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public static class AudioUtil
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.AudioUtil");



        static MethodInfo? m_HasAudioCallback;
        static readonly object[] mp_HasAudioCallback = new object[1];
        public static bool HasAudioCallback(MonoBehaviour monoBehaviour)
        {
            m_HasAudioCallback ??= type.GetMethod("HasAudioCallback", BindingFlags.Public | BindingFlags.Static);

            mp_HasAudioCallback[0] = monoBehaviour;
            return (bool)m_HasAudioCallback.Invoke(null, mp_HasAudioCallback);
        }

        static MethodInfo? m_GetCustomFilterChannelCount;
        static readonly object[] mp_GetCustomFilterChannelCount = new object[1];
        public static int GetCustomFilterChannelCount(MonoBehaviour monoBehaviour)
        {
            m_GetCustomFilterChannelCount ??= type.GetMethod("GetCustomFilterChannelCount", BindingFlags.Public | BindingFlags.Static);

            mp_GetCustomFilterChannelCount[0] = monoBehaviour;
            return (int)m_GetCustomFilterChannelCount.Invoke(null, mp_GetCustomFilterChannelCount);
        }
    }
}

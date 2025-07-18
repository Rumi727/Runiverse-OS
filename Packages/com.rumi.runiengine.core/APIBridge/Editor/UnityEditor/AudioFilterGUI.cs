#nullable enable
using System;
using System.Reflection;
using UnityEngine;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public sealed class AudioFilterGUI
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.AudioFilterGUI");

        public static AudioFilterGUI CreateInstance() => new AudioFilterGUI(Activator.CreateInstance(type));
        public static AudioFilterGUI GetInstance(object instance) => new AudioFilterGUI(instance);

        AudioFilterGUI(object instance) => this.instance = instance;

        public object instance { get; }



        public EditorGUI.VUMeter.SmoothingData[]? dataOut
        {
            get
            {
                f_dataOut ??= type.GetField("dataOut", BindingFlags.NonPublic | BindingFlags.Instance);

                Array array = (Array)f_dataOut.GetValue(instance);
                if (_dataOut == null || _dataOut.Length != array.Length)
                    _dataOut = new EditorGUI.VUMeter.SmoothingData[array.Length];

                Array.Copy(array, _dataOut, array.Length);
                return _dataOut;
            }
            set
            {
                f_dataOut ??= type.GetField("dataOut", BindingFlags.Public | BindingFlags.Instance);
                f_dataOut.SetValue(instance, value);
            }
        }
        static EditorGUI.VUMeter.SmoothingData[]? _dataOut;
        static FieldInfo? f_dataOut;



        static MethodInfo? m_DrawAudioFilterGUI;
        static readonly object[] mp_DrawAudioFilterGUI = new object[1];
        public void DrawAudioFilterGUI(MonoBehaviour monoBehaviour)
        {
            m_DrawAudioFilterGUI ??= type.GetMethod("DrawAudioFilterGUI", BindingFlags.Public | BindingFlags.Instance);
            mp_DrawAudioFilterGUI[0] = monoBehaviour;

            /* 
             * 어째서인지 Null 참조 예외가 발생하지만 이는 유니티 버그로 추정됨
             * 모든 변수 값을 확인해봐도 null이 아님
             */
            try
            {
                m_DrawAudioFilterGUI.Invoke(instance, mp_DrawAudioFilterGUI);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException.GetType() != typeof(NullReferenceException))
                    throw;
            }
        }



        public override string ToString() => instance.ToString();
    }
}

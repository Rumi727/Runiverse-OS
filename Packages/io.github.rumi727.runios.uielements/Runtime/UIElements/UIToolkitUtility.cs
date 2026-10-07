#nullable enable
using RuniOS.Reflection;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public static class UIElementsUtility
    {
        /// <summary>
        /// RuniOS 컨트롤 스타일
        /// </summary>
        public static ThemeStyleSheet rosControlStyle
        {
            get
            {
                if (_rosControlStyle == null)
                    _rosControlStyle = Resources.Load<ThemeStyleSheet>("RuniOS/UI Elements/ROS Control Style");

                return _rosControlStyle;
            }
        }
        static ThemeStyleSheet? _rosControlStyle;

        /// <summary>
        /// RuniOS 에디터 테마
        /// </summary>
        public static ThemeStyleSheet rosEditorTheme
        {
            get
            {
                if (_rosEditorTheme == null)
                    _rosEditorTheme = Resources.Load<ThemeStyleSheet>("RuniOS/UI Elements/Editor/ROS Editor Theme");

                return _rosEditorTheme;
            }
        }
        static ThemeStyleSheet? _rosEditorTheme;

        public static void SetValueWithoutNotify<T>(this INotifyValueChanged<T> element, T newValue) => element.SetValueWithoutNotify(newValue);

        static readonly ConditionalWeakTable<CallbackEventHandler, Dictionary<(Type type, ValueChangedCallback callback), Delegate>> registeredValueChangedCallbacks = [];
        public static bool RegisterValueChangedCallback(this CallbackEventHandler element, Type targetType, ValueChangedCallback callback)
        {
            if (!typeof(INotifyValueChanged<>).MakeGenericType(targetType).IsInstanceOfType(element))
                return false;

            // AOT 환경에선 작동하지 않지만 IL2CPP에선 작동합니다.
            MethodInfo methodInfo = ReflectionUtility.GetMethodInfo(Callback<object>).GetGenericMethodDefinition();
            return (bool)methodInfo.MakeGenericMethod(targetType).Invoke(null, [element, callback]);

            static bool Callback<T>(INotifyValueChanged<T> control, ValueChangedCallback callback)
            {
                Dictionary<(Type, ValueChangedCallback), Delegate> callbacks = registeredValueChangedCallbacks.GetOrCreateValue((CallbackEventHandler)control);

                var key = (typeof(T), callback);
                if (callbacks.ContainsKey(key))
                    return true;

                EventCallback<ChangeEvent<T>> wrapper = x => callback.Invoke(x.previousValue, x.newValue);
                callbacks.Add(key, wrapper);

                return control.RegisterValueChangedCallback(wrapper);
            }
        }

        public static bool UnregisterValueChangedCallback(this CallbackEventHandler element, Type targetType, ValueChangedCallback callback)
        {
            if (!typeof(INotifyValueChanged<>).MakeGenericType(targetType).IsInstanceOfType(element))
                return false;

            // AOT 환경에선 작동하지 않지만 IL2CPP에선 작동합니다.
            MethodInfo methodInfo = ReflectionUtility.GetMethodInfo(Callback<object>).GetGenericMethodDefinition();
            return (bool)methodInfo.MakeGenericMethod(targetType).Invoke(null, [element, callback]);

            static bool Callback<T>(INotifyValueChanged<T> control, ValueChangedCallback callback)
            {
                if (!registeredValueChangedCallbacks.TryGetValue((CallbackEventHandler)control, out var callbacks))
                    return true;

                var key = (typeof(T), callback);
                if (!callbacks.Remove(key, out Delegate? wrapper))
                    return true;

                if (callbacks.Count <= 0)
                    registeredValueChangedCallbacks.Remove((CallbackEventHandler)control);

                return control.UnregisterValueChangedCallback((EventCallback<ChangeEvent<T>>)wrapper);
            }
        }
    }
}
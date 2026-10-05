using System;
using System.Reflection;

namespace RuniOS.Editor.APIBridge.UnityEngine.UIElements
{
    public partial interface IPrefixLabelBridge
    {
        /// <summary>
        /// 지정된 문자열 값으로 레이블 속성을 설정합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 Unity 내부 코드로의 <b>브릿지가 아니며</b>, 편의를 위해 직접 작성되었습니다.<br/>
        /// This method is <b>not a bridge</b> to Unity's internal code, and is written directly for convenience.
        /// <br/><br/>
        /// 내부적으로는 `__instance` 객체의 `label` 속성을 직접 설정하려고 시도합니다. 만약 `label` 속성을 찾거나 설정할 수 없는 경우, `INotifyValueChanged&lt;string&gt;` 인터페이스를 통해 `labelElement`의 값을 알림 없이 설정합니다.
        /// </remarks>
        /// <param name="value">설정할 레이블의 새로운 값입니다. null일 경우 빈 문자열로 처리됩니다.</param>
        /// <exception cref="TargetInvocationException">`propertyInfo.SetValue` 호출 시 예외가 발생하면 이 예외가 발생할 수 있습니다.</exception>
        /// <exception cref="ArgumentException">`propertyInfo.SetValue` 호출 시 예외가 발생하면 이 예외가 발생할 수 있습니다.</exception>
        /// <exception cref="MethodAccessException">`propertyInfo.SetValue` 호출 시 예외가 발생하면 이 예외가 발생할 수 있습니다.</exception>
        /// <exception cref="NullReferenceException">`__instance` 또는 `labelElement`가 null일 경우 발생할 수 있습니다.</exception>
        public void SetLabel(string? value)
        {
            PropertyInfo? propertyInfo = __instance.GetType().GetProperty(nameof(label));
            if (propertyInfo != null && propertyInfo.SetMethod != null)
                propertyInfo.SetValue(__instance, value);
            else
                labelElement.text = value ?? string.Empty;
        }
    }
}
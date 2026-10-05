using System;
using UnityEngine;

namespace RuniOS.Editor.APIBridge.UnityEngine.UIElements
{
    public partial class BaseFieldBridge<TValueType>
    {
#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
        public Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated
        {
            get
            {
                if (__instance.expressionEvaluated == null)
                    return null;

                Action<ExpressionEvaluator.Expression> clonedAction = __instance.expressionEvaluated;
                return Invoke;
                
                void Invoke(ExpressionEvaluatorBridge.ExpressionBridge obj) => clonedAction.Invoke((ExpressionEvaluator.Expression)obj.__instance);
            }
            set
            {
                if (value != null)
                    __instance.expressionEvaluated = Invoke;
                else
                    __instance.expressionEvaluated = null;
                
                void Invoke(ExpressionEvaluator.Expression obj) => value.Invoke(ExpressionEvaluatorBridge.ExpressionBridge.__GetInstanceFrom(obj));
            }
        }
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
    }
}
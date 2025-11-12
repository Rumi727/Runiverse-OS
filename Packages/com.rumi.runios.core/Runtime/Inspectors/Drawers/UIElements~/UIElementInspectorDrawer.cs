#nullable enable
using RuniOS.UIElements;
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements
{
    public abstract class UIElementInspectorDrawer : InspectorDrawer
    {
        public static UIElementInspectorDrawer FindDrawer(IInspectorVariableElement element, Inspector? rootInspector = null, Func<(Type type, CustomInspectorDrawerAttribute attribute), bool>? predicate = null)
        {
            Type? type = AttributeDrawer<UIElementInspectorDrawer, CustomInspectorDrawerAttribute>.FindDrawerType(element.variableType, predicate);
            if (type != null)
                return (UIElementInspectorDrawer)Activator.CreateInstance(type, element, rootInspector);

            return new ObjectInspectorDrawer(element, rootInspector);
        }
        
        /// <summary>
        /// 루트 인스펙터를 가져옵니다.
        /// </summary>
        public Inspector? rootInspector { get; }

        /// <summary>
        /// UI 요소를 빌드합니다.<br/>
        /// <see cref="Bind"/> 메소드는 호출해선 안됩니다. (<see cref="Inspector"/> 같은 상위 클래스에서 자동으로 호출합니다.)
        /// </summary>
        public abstract VisualElement Build(string label = "", InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List);
        
        static readonly object?[] setValueWithoutNotifyMethodPar = new object?[1];

        /// <summary>
        /// UI 요소를 변수에 바인딩하기 위한 콜백을 등록합니다.<br/>
        /// 이 메서드는 서브클래스에서 다른 바인딩 구현을 위해 오버라이드할 수 있습니다.
        /// </summary>
        /// <param name="visualElement">바인딩할 UI 요소입니다.</param>
        /// <param name="flags"></param>
        /// <param name="readAction">변수의 값을 읽어 UI 요소에 적용하는 액션입니다.
        /// 이 액션은 <b>수동으로</b> 호출되어야 UI 요소에 초기 값 또는 변수가 변경되었을 때의 값을 업데이트할 수 있습니다.</param>
        /// <exception cref="ArgumentNullException"><paramref name="visualElement"/>가 <see langword="null"/>일 경우 발생합니다.</exception>
        public virtual void Bind(VisualElement visualElement, InspectorFlags flags, out Action? readAction)
        {
            if (visualElement == null)
                throw new ArgumentNullException(nameof(visualElement));
            
            readAction = null;

            if (variableElement == null)
                return;

            try
            {
                if (visualElement.GetType().IsAssignableToGenericDefinition(typeof(INotifyValueChanged<>), out Type? resolvedType))
                {
                    MethodInfo? method = AccessUtility.DeclaredMethod(resolvedType, nameof(INotifyValueChanged<int>.SetValueWithoutNotify));
                    if (method != null)
                    {
                        readAction = Read;

                        void Read()
                        {
                            bool isReadable = !variableElement.inspectable.instancesIsEmpty && variableElement.IsReadable(flags);
                            visualElement.enabledSelf = variableElement.inspectable.instancesIsEmpty || !variableElement.IsWritable(flags);
                            
                            try
                            {
                                setValueWithoutNotifyMethodPar[0] = isReadable ? variableElement.value : variableElement.variableType.GetDefaultValue();
                                method.Invoke(visualElement, setValueWithoutNotifyMethodPar);
                                
                                if (visualElement is IMixedValueSupport mixedValueSupport)
                                    mixedValueSupport.showMixedValue = !isReadable || variableElement.isMixedValue;
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                Debug.LogWarning("An exception occurred when reading the value of a property, preventing the control value from being modified.");
                            }
                        }
                    }
                    else
                        Debug.LogWarning($"Method not found: '{nameof(INotifyValueChanged<int>.SetValueWithoutNotify)}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogWarning("An exception occurred while registering a read event on the control, so registration failed.");
            }
            
            try
            {
                visualElement.RegisterValueChangedCallback(variableElement.variableType, Write);
                    
                void Write(object fieldValue)
                {
                    if (variableElement.inspectable.instancesIsEmpty || !variableElement.IsWritable(flags))
                        return;
                    
                    try
                    {
                        variableElement.value = fieldValue;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogWarning("An exception occurred while executing the control's write event, preventing the actual value of the property from being modified.");
                    }
                    
                    rootInspector?.Update();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogWarning("An exception occurred while registering a write event on the control, so registration failed.");
            }
        }

        protected UIElementInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element) => this.rootInspector = rootInspector;
        protected UIElementInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList) => this.rootInspector = rootInspector;
    }
}
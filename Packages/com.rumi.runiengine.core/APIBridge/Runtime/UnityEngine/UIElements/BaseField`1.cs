#nullable enable
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public class BaseField<TValueType> : BindableElement, INotifyValueChanged<TValueType>, IMixedValueSupport, IPrefixLabel, IEditableElement
    {
        public static new Type type { get; } = typeof(global::UnityEngine.UIElements.BaseField<TValueType>);

        public static BaseField<TValueType> GetInstance(global::UnityEngine.UIElements.BaseField<TValueType> instance) => new BaseField<TValueType>(instance);

        protected BaseField(global::UnityEngine.UIElements.BaseField<TValueType> instance) : base(instance) => this.instance = instance;

        public new global::UnityEngine.UIElements.BaseField<TValueType> instance { get; set; }



        public string label => IPrefixLabel.GetInstance(instance).label;
        public Label labelElement => IPrefixLabel.GetInstance(instance).labelElement;

        public TValueType value
        {
            get => instance.value;
            set => instance.value = value;
        }

        public bool showMixedValue
        {
            get => instance.showMixedValue;
            set => instance.showMixedValue = value;
        }

        public global::UnityEngine.UIElements.VisualElement visualInput
        {
            get
            {
                f_visualInput ??= type.GetProperty("visualInput", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return (global::UnityEngine.UIElements.VisualElement)f_visualInput!.GetValue(instance);
            }
            set
            {
                f_visualInput ??= type.GetProperty("visualInput", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                f_visualInput!.SetValue(instance, value);
            }
        }
        // ReSharper disable once StaticMemberInGenericType
        static PropertyInfo? f_visualInput;

        public Action editingStarted
        {
            get => IEditableElement.GetInstance(instance).editingStarted;
            set => IEditableElement.GetInstance(instance).editingStarted = value;
        }
        public Action editingEnded 
        {
            get => IEditableElement.GetInstance(instance).editingEnded;
            set => IEditableElement.GetInstance(instance).editingEnded = value;
        }

        public void SetValueWithoutNotify(TValueType newValue) => instance.SetValueWithoutNotify(newValue);
    }
}

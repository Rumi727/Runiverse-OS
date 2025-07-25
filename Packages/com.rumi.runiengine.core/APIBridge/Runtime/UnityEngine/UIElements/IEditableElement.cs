#nullable enable
using System;
using System.Reflection;

namespace RuniEngine.APIBridge.UnityEngine.UIElements
{
    public interface IEditableElement
    {
        public static Type type { get; } = AssemblyManager.UnityEngine_CoreModule.GetType("UnityEngine.UIElements.IEditableElement");

        public static IEditableElement GetInstance(object instance) => new EditableElement(Convert.ChangeType(instance, type));



        Action editingStarted { get; set; }
        Action editingEnded { get; set; }



        class EditableElement : IEditableElement
        {
            public EditableElement(object instance) => this.instance = instance;

            public object instance { get; }



            public Action editingStarted
            {
                get
                {
                    f_editingStarted ??= type.GetProperty("editingStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return (Action)f_editingStarted!.GetValue(instance);
                }
                set
                {
                    f_editingStarted ??= type.GetProperty("editingStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    f_editingStarted!.SetValue(instance, value);
                }
            }
            static PropertyInfo? f_editingStarted;

            public Action editingEnded
            {
                get
                {
                    f_editingEnded ??= type.GetProperty("editingEnded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    return (Action)f_editingEnded!.GetValue(instance);
                }
                set
                {
                    f_editingEnded ??= type.GetProperty("editingEnded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    f_editingEnded!.SetValue(instance, value);
                }
            }
            static PropertyInfo? f_editingEnded;
        }
    }
}

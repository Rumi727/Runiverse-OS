#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using BridgeTarget = System.Object;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public interface IEditableElement
    {
        static Type type { get; } = AssemblyManager.UnityEngine_CoreModule.GetType("UnityEngine.UIElements.IEditableElement");

        private static readonly ConditionalWeakTable<BridgeTarget, EditableElement> cached = new ConditionalWeakTable<BridgeTarget, EditableElement>();
        static IEditableElement GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out EditableElement? element))
            {
                element = new EditableElement(Convert.ChangeType(instance, type));
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }



        Action editingStarted { get; set; }
        Action editingEnded { get; set; }



        private class EditableElement : IEditableElement
        {
            public EditableElement(BridgeTarget instance) => this.instance = instance;

            public BridgeTarget instance { get; set; }



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

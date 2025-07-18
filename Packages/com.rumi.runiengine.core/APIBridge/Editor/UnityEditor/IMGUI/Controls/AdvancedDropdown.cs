#nullable enable
using System;
using System.Reflection;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UniAdvancedDropdown = UnityEditor.IMGUI.Controls.AdvancedDropdown;

namespace RuniEngine.Editor.APIBridge.UnityEditor.IMGUI.Controls
{
    public class AdvancedDropdown
    {
        public static Type type { get; } = typeof(UniAdvancedDropdown);

        public static AdvancedDropdown GetInstance(UniAdvancedDropdown instance) => new AdvancedDropdown(instance);

        AdvancedDropdown(UniAdvancedDropdown instance) => this.instance = instance;

        public UniAdvancedDropdown instance { get; set; }

        public AdvancedDropdownState m_State
        {
            get
            {
                f_m_State ??= type.GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance);
                return (AdvancedDropdownState)f_m_State.GetValue(instance);
            }
            set
            {
                f_m_State ??= type.GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance);
                f_m_State.SetValue(instance, value);
            }
        }
        static FieldInfo? f_m_State;

        public Vector2 maximumSize
        {
            get
            {
                f_maximumSize ??= type.GetProperty("maximumSize", BindingFlags.NonPublic | BindingFlags.Instance);
                return (Vector2)f_maximumSize.GetValue(instance);
            }
            set
            {
                f_maximumSize ??= type.GetProperty("maximumSize", BindingFlags.NonPublic | BindingFlags.Instance);
                f_maximumSize.SetValue(instance, value);
            }
        }
        static PropertyInfo? f_maximumSize;
    }
}

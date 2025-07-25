#nullable enable
using System;
using System.Reflection;
using UniSerializedObject = UnityEditor.SerializedObject;

namespace RuniEngine.Editor.APIBridge.UnityEditor
{
    public class SerializedObject
    {
        public static Type type { get; } = typeof(UniSerializedObject);

        public static SerializedObject GetInstance(UniSerializedObject instance) => new SerializedObject(instance);

        SerializedObject(UniSerializedObject instance) => this.instance = instance;

        public UniSerializedObject instance { get; set; }



        public bool isValid
        {
            get
            {
                f_isValid ??= type.GetProperty("isValid", BindingFlags.NonPublic | BindingFlags.Instance);
                return (bool)f_isValid!.GetValue(instance);
            }
        }
        static PropertyInfo? f_isValid;

        public override string ToString() => instance.ToString();
    }
}

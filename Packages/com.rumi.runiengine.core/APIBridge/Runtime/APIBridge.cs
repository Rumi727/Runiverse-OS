#nullable enable
#if UNITY_EDITOR
using System;
using System.Reflection;

/*
 * APIBridge 템플릿
 * 
 * 수식어 설명
 * 위에서부터 아래 순서대로 왼쪽에서 부터 오른쪽으로 적기
 * 
 * f : Field, Property
 * m : Method
 * p : Parameter
 * t : Type
 */

namespace RuniEngine.APIBridge
{
    class APIBridge
    {
        public static Type type { get; } = typeof(APIBridge);

        public static APIBridge CreateInstance() => new APIBridge(Activator.CreateInstance(type));
        public static APIBridge GetInstance(object instance) => new APIBridge(instance);

        APIBridge(object instance) => this.instance = instance;

        public object instance { get; }



        public object fieldName
        {
            get
            {
                f_fieldName ??= type.GetField("fieldName", BindingFlags.NonPublic | BindingFlags.Instance);
                return f_fieldName.GetValue(instance);
            }
            set
            {
                f_fieldName ??= type.GetField("fieldName", BindingFlags.NonPublic | BindingFlags.Instance);
                f_fieldName.SetValue(instance, value);
            }
        }
        static FieldInfo? f_fieldName;



        static MethodInfo? m_MethodName;
        static readonly object[] mp_MethodName = new object[1];
        static readonly Type[] mpt_MethodName = new Type[] { typeof(object) };
        public void MethodName(object p)
        {
            m_MethodName ??= type.GetMethod("MethodName", BindingFlags.NonPublic | BindingFlags.Instance, null, mpt_MethodName, null);

            mp_MethodName[0] = p;
            m_MethodName.Invoke(instance, mp_MethodName);
        }



        public override string ToString() => instance.ToString();
    }
}
#endif
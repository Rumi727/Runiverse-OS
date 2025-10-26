#nullable enable
using RuniOS.Editor.Inspectors;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Windows
{
    public sealed class InspectorTestPanel : ScriptableObject, IControlPanel
    {
        public string label => "control_panel.inspector_test";

        public int sort => 500;

        public bool allowUpdate => true;
        public bool allowUpdateInEditor => true;

        readonly Test test = new Test();
        readonly Inspector inspector = new Inspector();

        void OnEnable() => inspector.Rebuild(new InspectableObject(test));

        public void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            InspectorFlags flags = (InspectorFlags)EditorGUILayout.EnumFlagsField("Inspector Flags", inspector.inspectorFlags);
            if (EditorGUI.EndChangeCheck())
                inspector.Rebuild(inspector.inspectable ?? new InspectableObject(test), flags);
            
            inspector.DrawLayout();
        }

        public class Test
        {
            public bool boolField = false;
            public sbyte sbyteField = 42;
            public byte byteField = 42;
            public short shortField = 42;
            public ushort ushortField = 42;
            public int intField = 42;
            public uint uintField = 42;
            public long longField = 42;
            public Test2 test2Field = new Test2();
            public Test2? nullableTest2Field = new Test2();
            public readonly Test2 readonlyTest2Field = new Test2();
            public Test2 privateReadOnlyTest2Property { get; private set; } = new Test2();
            public ulong ulongField = 42;
            public float floatField = 42;
            public double doubleField = 42;
            public decimal decimalField = 42;
            public nint nintField = 42;
            public nuint nuintField = 42;
            public char charField = 'a';
            public string stringField = "text";
            public int? nullableInt = 42;
            public List<Test2> test2List = new();
            public List<List<Test2>> test2ListList = new();
            public Test2[] test2Array = new Test2[] { new(), new(), new(), new() };
            public List<Test2?> nullableTest2List = new();
            public List<List<Test2>?> nullableTest2ListList = new();
            public List<List<Test2?>?> nullableNullableTest2ListList = new();
            public Test2?[] nullableTest2Array = new Test2?[] { new(), new(), new(), new() };
            public List<List<Test2?>?>? nullableNullableTest2ListNullableList = new();
            public Test2?[]? nullableTest2NullableArray = new Test2?[] { new(), new(), new(), new() };
            public SerializableNullable<int> serializableNullableInt = 42;
            public SerializableNullable<SerializableNullable<float>> serializableNullableNullableFloat = new(42);
            public StructTest structTest = new StructTest();
            public StructTest? nullableStructTest = new StructTest();
            public Vector2 vector2;
            public Vector3 vector3;
            public Vector4 vector4;
            public Rect rect;
            public Color color;
            public StructTest2? nullableStructTest2;
            public SerializableNullable<StructTest2> serializableNullableStructTest2;

            public class Test2
            {
                public float test2Field = 100;
                public double doubleProperty { get; set; } = 32;
                public double privateReadOnlyProperty { get; private set; } = 64;
                public float test3Field = 100;
                public double writeOnlyProperty { set => _writeOnlyProperty = value; }
                // ReSharper disable once NotAccessedField.Local
                double _writeOnlyProperty = 64;
                public float test4Field = 100;

                public Test2? nullableTest2Property { get; set; }
                public Test2? writeOnlyNullableTest2Property { set => _writeOnlyNullableTest2Property = value; }
                // ReSharper disable once NotAccessedField.Local
                Test2? _writeOnlyNullableTest2Property;
            }

            public struct StructTest
            {
                public float test2Field;
                public double doubleProperty { get; set; }
                public float test3Field;
                public double writeOnlyProperty { set => _writeOnlyProperty = value; }
                // ReSharper disable once NotAccessedField.Local
                double _writeOnlyProperty;
                public float test4Field;

                public StructTest2 structTest2;
                public StructTest2? nullableStructTest2;
                public SerializableNullable<StructTest2> serializableNullableStructTest2;
            }
            
            public struct StructTest2
            {
                public float test2Field;
                public double doubleProperty { get; set; }
                public float test3Field;
                public double writeOnlyProperty { set => _writeOnlyProperty = value; }
                // ReSharper disable once NotAccessedField.Local
                double _writeOnlyProperty;
                public float test4Field;
            }
        }
    }
}
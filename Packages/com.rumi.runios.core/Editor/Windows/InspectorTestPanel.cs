#nullable enable
using RuniOS.Editor.Inspectors;
using RuniOS.Inspectors.Csharp;
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

        public void OnGUI() => inspector.DrawLayout();

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
            public Test2 test2 = new Test2();
            public ulong ulongField = 42;
            public float floatField = 42;
            public double doubleField = 42;
            public decimal decimalField = 42;
            public nint nintField = 42;
            public nuint nuintField = 42;
            public char charField = 'a';
            public string stringField = "text";

            public class Test2
            {
                public float test2Field = 100;
                public double doubleProperty { get; set; } = 32;
                public double readOnlyProperty { get; } = 64;
                public float test3Field = 100;
                public double writeOnlyProperty { set => _writeOnlyProperty = value; }
                // ReSharper disable once NotAccessedField.Local
                double _writeOnlyProperty = 64;
                public float test4Field = 100;
            }
        }
    }
}
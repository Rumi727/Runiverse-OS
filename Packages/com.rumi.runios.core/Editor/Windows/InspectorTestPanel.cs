#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Editor.Inspectors;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Localizations;
using RuniOS.Resource;
using System.Collections;

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

        PackIdentifier packIdentifier = PackIdentifier.CreateByID(Identifier.empty);

        void OnEnable() => inspector.Rebuild(new InspectableObject(test));

        public void OnGUI()
        {
            BeginWideMode();
            BeginHierarchyMode();

            packIdentifier = PackIdentifierFieldLayout("Pack Identifier", packIdentifier);
            
            EditorGUI.BeginChangeCheck();
            InspectorFlags flags = (InspectorFlags)EditorGUILayout.EnumFlagsField("Inspector Flags", inspector.inspectorFlags);
            if (EditorGUI.EndChangeCheck())
                inspector.Rebuild(inspector.inspectable ?? new InspectableObject(test), flags);
            
            inspector.DrawLayout(new Vector2(15, 0));
            EndHierarchyMode();
            EndWideMode();
        }
        
#pragma warning disable CS0414 // 필드가 대입되었으나 값이 사용되지 않습니다
        internal class Test
        {
            /*public unsafe int* pointer = (int*)new IntPtr(15335321);
            public unsafe int* nullPointer = null;*/
            public static Test2? staticTest2Field;
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
            public Test2? readonlyNullableTest2Property { get; } = new Test2();
            public Test2? privateReadOnlyNullableTest2Property { get; private set; } = new Test2();
            public ulong ulongField = 42;
            public float floatField = 42;
            public double doubleField = 42;
            public decimal decimalField = 42;
            public nint nintField = 42;
            public nuint nuintField = 42;
            public char charField = 'a';
            public string stringField = "text";
            public int? nullableInt = 42;
            public TextAnchor textAnchor = TextAnchor.UpperLeft;
            public InspectorFlags inspectorFlags = InspectorFlags.None;
            public List<Test2> test2List = [];
            public List<List<Test2>> test2ListList = [];
            public Test2[] test2Array = [new(), new(), new(), new()];
            public List<Test2?> nullableTest2List = [];
            public List<List<Test2>?> nullableTest2ListList = [];
            public List<List<Test2?>?> nullableNullableTest2ListList = [];
            public Test2?[] nullableTest2Array = [new(), new(), new(), new()];
            public List<List<Test2?>?>? nullableNullableTest2ListNullableList = [];
            public Test2?[]? nullableTest2NullableArray = [new(), new(), new(), new()];
            public SerializableNullable<int> serializableNullableInt = 42;
            public SerializableNullable<SerializableNullable<float>> serializableNullableNullableFloat = new(42);
            public StructTest structTest = new StructTest();
            public StructTest? nullableStructTest = new StructTest();
            public StructTest? readOnlyNullableStructTest { get; } = new StructTest();
            public StructTest? writeOnlyNullableStructTest { set => _writeOnlyNullableStructTest = value; }
            // ReSharper disable once NotAccessedField.Local
            StructTest? _writeOnlyNullableStructTest;
            public Vector2 vector2;
            public Vector2Int vector2Int;
            public Vector3 vector3;
            public Vector3Int vector3Int;
            public Vector4 vector4;
            public Rect rect;
            public RectInt rectInt;
            public Color color;
            public Color32 color32;
            public Bounds bounds;
            public BoundsInt boundsInt;
            public CornerRadius cornerRadius;
            public SerializableType serializableType;
            public Type? type;
            public Version version;
            public VersionRange versionRange;
            public HexColor hexColor;
            public RectOffset rectOffset;
            public RectVertices rectVertices;
            public Identifier identifier;
            public PackIdentifier packIdentifier;
            public ResourceKey resourceKey;
            public AssetRef<LocalizationData> assetRef;
            public PhysicalPath physicalPath;
            public RuniPath path;
            public FileExtension fileExtension;
            public StructTest2? nullableStructTest2;
            public SerializableNullable<StructTest2> serializableNullableStructTest2;
            public Dictionary<string, Test2> dictionary = new() { { "wa sans", new Test2() } };
            public SerializableDictionary<string, int> serializableDictionary = new() { { "wa sans", 0 } };
            public readonly HashSet<Test2> hashSet = [new Test2()];
            public readonly Queue<Test2> queue = new();
            public readonly Stack<Test2> stack = new();
            public ReadOnlySet<Test2> readOnlySet;
            public ReadOnlyQueue<Test2> readOnlyQueue;
            public ReadOnlyStack<Test2> readOnlyStack;
            public readonly HashSet<Vector2> hashSetVector2 = [new Vector2()];
            public readonly Queue<Vector2> queueVector2 = new();
            public readonly Stack<Vector2> stackVector2 = new();
            public ReadOnlySet<Vector2> readOnlySetVector2;
            public ReadOnlyQueue<Vector2> readOnlyQueueVector2;
            public ReadOnlyStack<Vector2> readOnlyStackVector2;
            public IList iList = new List<Test2> { new Test2() };
            public IList<Test2> iList2 = new List<Test2> { new Test2() };

            public Test()
            {
                readOnlySet = hashSet.AsReadOnly();
                readOnlyQueue = queue.AsReadOnly();
                readOnlyStack = stack.AsReadOnly();
                
                readOnlySetVector2 = hashSetVector2.AsReadOnly();
                readOnlyQueueVector2 = queueVector2.AsReadOnly();
                readOnlyStackVector2 = stackVector2.AsReadOnly();
            }

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
#pragma warning restore CS0414 // 필드가 대입되었으나 값이 사용되지 않습니다
    }
}
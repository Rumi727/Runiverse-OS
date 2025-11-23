#nullable enable
using RuniOS;
using RuniOS.Collections.Generic;
using RuniOS.Inspectors;
using RuniOS.IO;
using RuniOS.Resource;
using RuniOS.Utility.Attributes;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public sealed class Test : MonoBehaviour
{
    public string a { get => _a; set => _a = value; }
    [SerializeField] string _a = string.Empty;

    public List<Test2> list = new List<Test2>();
    public List<string> stringList = new List<string>();
    public List<StringListTest> stringList2 = new List<StringListTest>();
    public List<StringListTestStruct> stringList3 = new List<StringListTestStruct>();
    public SerializableDictionary<string, Object> serializableDictionary = new SerializableDictionary<string, Object>();
    public SerializableDictionary<string, int> serializableDictionary2 = new SerializableDictionary<string, int>();
    public SerializableDictionary<string, SerializableDictionary<string, Test2>> serializableDictionary3 = new();

    public Test2 test2 = new Test2();
    [SerializeReference] public Test2 test3Ref = new Test2();
    public Vector3 vector3;
    public Vector4 vector4;
    public Version version;
    public VersionRange versionRange;
    public Identifier identifier;
    [NullableField("custom null text")] public SerializableNullable<int> nInt;
    public SerializableNullable<uint> nUInt;
    public SerializableNullable<long> nLong;
    public SerializableNullable<float> nFloat;
    public SerializableNullable<double> nDouble;
    public SerializableNullable<decimal> nDecimal;
    [NullableField("custom null text 2")] public SerializableNullable<decimal> nDecimal2;
    public SerializableNullable<Identifier> nIdentifier;
    public SerializableNullable<Vector2> nVector2;
    public SerializableNullable<Vector3> nVector3;
    public SerializableNullable<Vector4> nVector4;
    public SerializableNullable<Rect> nRect;
    public SerializableNullable<Color> nColor;
    public SerializableNullable<TextAlignment> nEnum;
    [ReadOnlyField] public SerializableNullable<char> nChar = 'a';
    public char @char = 'a';
    [Range(10, 20)] public float slider;
    public FilePath filePath;
    public FileExtension fileExtension;
    public HexColor hexColor;
    public RectCorner rectCorner;
    public RuniOS.RectOffset rectOffset;
    public UnlimitedDateTime unlimitedDateTime;
    public SerializableKeyValuePair<string, SerializableNullable<double>> pair;
    public PackIdentifier packIdentifier;
    public TextAlignment textAlignment;
    public SerializableNullable<SerializableNullable<float>> nullableNullableFloat;
    [TypeField(typeof(TMP_Text))] public SerializableType type = typeof(Object);
    public SerializableType type2 = typeof(Object);
    [ReadOnlyField, TypeField(typeof(TMP_Text))] public SerializableType readOnlyType = typeof(TMP_Text);
    public SerializableNullable<RuniOS.RectOffset> nullableRectOffset;
    public float test;
    [NotNullField] public Object? uniObject;
    public InspectorFlags flags;
    
    void OnEnable() => DrivenPropertyManager.RegisterProperty(this, this, nameof(_a));

    void OnDisable() => DrivenPropertyManager.UnregisterProperty(this, this, nameof(_a));

    [System.Serializable]
    public class Test2
    {
        public SerializableNullable<Test3> test3 = new Test3();
        public float asdf;
        public Vector4 vector4;
        public Version version;
        public VersionRange versionRange;
        public Identifier identifier;
        [NullableField("null")] public SerializableNullable<int> nInt;
        public SerializableNullable<uint> nUInt;
        public SerializableNullable<long> nLong;
        public SerializableNullable<float> nFloat;
        public SerializableNullable<double> nDouble;
        public SerializableNullable<decimal> nDecimal;
        [NullableField("null")] public SerializableNullable<decimal> nDecimal2;
        public SerializableNullable<Identifier> nIdentifier;
        public SerializableNullable<Vector4> nVector4;
        public SerializableNullable<Rect> nRect;
        public SerializableNullable<Color> nColor;
        public SerializableNullable<TextAlignment> nEnum;
        public SerializableNullable<char> nChar = 'a';
        public char @char = 'a';
        [Range(10, 20)] public float slider;
        public FilePath filePath;
        public FileExtension fileExtension;
        public HexColor hexColor;
        public RectCorner rectCorner;
        public RuniOS.RectOffset rectOffset;
        public UnlimitedDateTime unlimitedDateTime;

        [System.Serializable]
        public struct Test3
        {
            public float asdf2;
            public float asdf3;
        }
    }

    [System.Serializable]
    public class StringListTest
    {
        public string test = "asdf";
    }

    [System.Serializable]
    public struct StringListTestStruct
    {
        public string test;
    }
}

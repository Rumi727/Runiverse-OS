#nullable enable
using RuniOS;
using RuniOS.Collections.Generic;
using RuniOS.IO;
using RuniOS.APIMarshal.UnityEngine.UIElements;
using RuniOS.Resource;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public sealed class Test : MonoBehaviour
{
    public UIDocument? document;

    public string a { get => _a; set => _a = value; }
    [SerializeField] string _a = string.Empty;

    [AnimFolder] public List<Test2> list = new List<Test2>();
    [AnimFolder] public List<string> stringList = new List<string>();
    [AnimFolder] public List<StringListTest> stringList2 = new List<StringListTest>();
    [AnimFolder] public List<StringListTestStruct> stringList3 = new List<StringListTestStruct>();
    public SerializableDictionary<string, Object> serializableDictionary = new SerializableDictionary<string, Object>();
    public SerializableDictionary<string, int> serializableDictionary2 = new SerializableDictionary<string, int>();
    public SerializableDictionary<string, SerializableDictionary<string, Test2>> serializableDictionary3 = new();

    [AnimFolder] public Test2 test2 = new Test2();
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
    public SerializableNullable<Align> nEnum;
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
    [TypeField(typeof(Label))] public SerializableType type = typeof(Object);
    [ReadOnlyField, TypeField(typeof(Label))] public SerializableType readOnlyType = typeof(Label);
    public SerializableNullable<RuniOS.RectOffset> nullableRectOffset;
    public float test;
    [NotNullField] public Object? uniObject;
    
    void OnEnable() => DrivenPropertyManager.RegisterProperty(this, this, "_a");

    void Start()
    {
        if (!Application.isPlaying || document == null)
            return;
        
        Asdf asdf = new Asdf();
        document.rootVisualElement.Add(asdf);
        Debug.Log(asdf);
        
        Asdf2 asdf2 = new Asdf2("asdf", 3);
        document.rootVisualElement.Add(asdf2);
        Debug.Log(asdf2);

        document.rootVisualElement.Add(new Button() { text = "asdf" });
        document.rootVisualElement.Q<ListView>("test").itemsSource = new List<Vector2>();
        document.rootVisualElement.Query<TextElement>();
        document.rootVisualElement.schedule.Execute(static x => { }).Every(0);

        _ = nInt.Value;
    }

    void OnDisable() => DrivenPropertyManager.UnregisterProperty(this, this, "_a");

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
        public SerializableNullable<Align> nEnum;
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

    public class Asdf2 : BaseCompositeFieldMarshal<string, IntegerField, int>
    {
        public Asdf2(string label, int fieldsByLine) : base(label, fieldsByLine)
        {
            
        }

        public override IEnumerable<FieldDescriptionMarshal> DescribeFieldsMarshal()
        {
            yield return new FieldDescriptionMarshal("asdf", "asdf", static x => 0, static (ref string val, int fieldValue) => { });
            yield return new FieldDescriptionMarshal("asdf", "asdf", static x => 0, static (ref string val, int fieldValue) => { });
            yield return new FieldDescriptionMarshal("asdf", "asdf", static x => 0, static (ref string val, int fieldValue) => { });
        }
    }

    public class Asdf : TextInputBaseFieldMarshal<string>.TextInputBaseMarshal
    {
        public Asdf()
        {
            Debug.Log("asdf");
        }
    }
}
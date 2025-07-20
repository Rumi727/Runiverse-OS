#nullable enable
using RuniEngine;
using RuniEngine.Collections.Generic;
using RuniEngine.Resource;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using DrivenPropertyManager = RuniEngine.APIBridge.UnityEngine.DrivenPropertyManager;

[ExecuteAlways]
public sealed class Test : MonoBehaviour
{
    public UIDocument document;

    public string a { get => _a; set => _a = value; }
    [SerializeField] string _a;
    public SerializableDictionary<string, Object> serializableDictionary = new SerializableDictionary<string, Object>();
    public SerializableDictionary<string, int> serializableDictionary2 = new SerializableDictionary<string, int>();

    public Test2 test2;
    [SerializeReference] public Test2 test3Ref = new Test2();
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

    void OnEnable()
    {
        DrivenPropertyManager.RegisterProperty(this, this, "_a");
    }

    void Start()
    {
        if (!Application.isPlaying)
            return;

        document.rootVisualElement.Add(new Button() { text = "asdf" });
        document.rootVisualElement.Q<ListView>("test").itemsSource = new List<Vector2>();
        document.rootVisualElement.Query<TextElement>();
        document.rootVisualElement.schedule.Execute(static x => { }).Every(0);
    }

    void OnDisable()
    {
        DrivenPropertyManager.UnregisterProperty(this, this, "_a");
    }

    [System.Serializable]
    public class Test2
    {
        public Test3 test3;
        public float asdf;

        [System.Serializable]
        public class Test3
        {
            public float asdf2;
        }
    }
}


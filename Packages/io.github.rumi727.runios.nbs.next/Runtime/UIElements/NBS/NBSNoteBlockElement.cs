#nullable enable
using RuniOS.NBS;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// Shows one note as a Note Block Studio block: the instrument texture faded by velocity, the key name, the key number, and a tint for fine pitch.<br/>
    /// 노트 하나를 Note Block Studio 블록처럼 보여줍니다. 벨로시티만큼 흐려지는 악기 텍스처, 음 이름, 키 번호, 미세 피치 틴트를 표시합니다.
    /// </summary>
    public class NBSNoteBlockElement : VisualElement
    {
        public const string ussClassName = "runios-nbs-note-block";
        public const string tintUssClassName = ussClassName + "__tint";
        public const string tintDownUssClassName = tintUssClassName + "--down";
        public const string tintUpUssClassName = tintUssClassName + "--up";
        public const string keyUssClassName = ussClassName + "__key";
        public const string valueUssClassName = ussClassName + "__value";

        /// <summary>
        /// The number of block textures. Built-in instruments use their own index, and custom instruments repeat the remaining textures.<br/>
        /// 블록 텍스처 수입니다. 기본 악기는 자기 인덱스를 쓰고, 커스텀 악기는 나머지 텍스처를 반복해서 씁니다.
        /// </summary>
        public const int textureCount = 38;

        // Note Block Studio names keys from A0 at key 0; key 33 is F#3, the lowest key a note block plays.
        static readonly string[] keyNames = ["A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#"];

        const int lowestBlockKey = 33;
        const int highestBlockKey = 57;

        readonly VisualElement tint;
        readonly Label key;
        readonly Label value;

        /// <summary>
        /// Initializes an empty block.<br/>
        /// 빈 블록을 초기화합니다.
        /// </summary>
        public NBSNoteBlockElement()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;

            tint = new VisualElement { name = "tint", pickingMode = PickingMode.Ignore };
            tint.AddToClassList(tintUssClassName);
            Add(tint);

            key = new Label { name = "key", pickingMode = PickingMode.Ignore };
            key.AddToClassList(keyUssClassName);
            Add(key);

            value = new Label { name = "value", pickingMode = PickingMode.Ignore };
            value.AddToClassList(valueUssClassName);
            Add(value);
        }

        /// <summary>
        /// Gets the index of the block texture for an instrument.<br/>
        /// 악기의 블록 텍스처 인덱스를 가져옵니다.
        /// </summary>
        /// <param name="instrument">
        /// The instrument index of a note.<br/>
        /// 노트의 악기 인덱스입니다.
        /// </param>
        /// <returns>
        /// A value from 0 to <see cref="textureCount"/> - 1.<br/>
        /// 0부터 <see cref="textureCount"/> - 1 사이의 값을 반환합니다.
        /// </returns>
        public static int GetTextureIndex(int instrument)
        {
            if (instrument < NBSInstrument.vanillaCount)
                return Math.Max(0, instrument);

            return ((instrument - NBSInstrument.vanillaCount) % (textureCount - NBSInstrument.vanillaCount)) + NBSInstrument.vanillaCount;
        }

        /// <summary>
        /// Shows <paramref name="note"/> in this block.<br/>
        /// 이 블록에 <paramref name="note"/>를 표시합니다.
        /// </summary>
        /// <param name="note">
        /// The note to show.<br/>
        /// 표시할 노트입니다.
        /// </param>
        /// <param name="map">
        /// The map that <paramref name="note"/> belongs to, used to find what its instrument does.<br/>
        /// <paramref name="note"/>가 속한 맵이며, 악기의 기능을 찾는 데 사용합니다.
        /// </param>
        public void Bind(in NBSNote note, NBSMap map)
        {
            float alpha = 0.25f + (0.5f * note.velocity / 100f);
            style.unityBackgroundImageTintColor = new Color(1, 1, 1, alpha);

            tint.EnableInClassList(tintDownUssClassName, note.pitch < 0);
            tint.EnableInClassList(tintUpUssClassName, note.pitch > 0);
            tint.style.opacity = Math.Min(0.5f, Math.Abs(note.pitch) / 100f * alpha * 0.25f);

            key.text = keyNames[note.key % keyNames.Length] + ((note.key + 9) / keyNames.Length);
            value.text = map.GetFunction(note.instrument) switch
            {
                NBSInstrumentFunction.tempoChanger => $"{Math.Abs((int)note.pitch)} BPM",
                NBSInstrumentFunction.none when note.key < lowestBlockKey => "<",
                NBSInstrumentFunction.none when note.key > highestBlockKey => ">",
                NBSInstrumentFunction.none => (note.key - lowestBlockKey).ToString(),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Sets the block texture, or clears it with <see langword="null"/>.<br/>
        /// 블록 텍스처를 설정하며, <see langword="null"/>이면 지웁니다.
        /// </summary>
        /// <param name="texture">
        /// The texture to show.<br/>
        /// 표시할 텍스처입니다.
        /// </param>
        public void SetTexture(Texture2D? texture) => style.backgroundImage = texture != null ? Background.FromTexture2D(texture) : new StyleBackground(StyleKeyword.Null);
    }
}

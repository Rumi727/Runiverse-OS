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
        public const string mutedUssClassName = ussClassName + "--muted";
        public const string lockedUssClassName = ussClassName + "--locked";

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

        float baseAlpha;

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
        /// Gets the name of a key as Note Block Studio shows it, such as <c>F#3</c>. Key 0 is A0.<br/>
        /// Note Block Studio가 표시하는 키 이름을 가져옵니다(예: <c>F#3</c>). 키 0은 A0입니다.
        /// </summary>
        /// <param name="key">
        /// The key of a note.<br/>
        /// 노트의 키입니다.
        /// </param>
        public static string GetKeyName(int key) => keyNames[key % keyNames.Length] + ((key + 9) / keyNames.Length);

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
            float alpha = 0.25f + ((0.5f * note.velocity) / 100f);
            baseAlpha = alpha;
            style.unityBackgroundImageTintColor = new Color(1, 1, 1, alpha);

            tint.EnableInClassList(tintDownUssClassName, note.pitch < 0);
            tint.EnableInClassList(tintUpUssClassName, note.pitch > 0);
            tint.style.opacity = MathUtility.Min(0.5f, (note.pitch.Abs() / 100f) * alpha * 0.25f);

            key.text = GetKeyName(note.key);
            value.text = map.GetFunction(note.instrument) switch
            {
                NBSInstrumentFunction.tempoChanger => $"{Math.Abs((int)note.pitch)} BPM",
                NBSInstrumentFunction.soundStopper => GetStopperTargets(note),
                NBSInstrumentFunction.none when note.key < lowestBlockKey => "<",
                NBSInstrumentFunction.none when note.key > highestBlockKey => ">",
                NBSInstrumentFunction.none => (note.key - lowestBlockKey).ToString(),
                _ => string.Empty
            };
        }

        // A sound stopper stores the layers it stops in its pitch, pan, and volume, which Note Block Studio shows as a range. No start layer means every layer, and an end before the start means none.
        static string GetStopperTargets(in NBSNote note)
        {
            NBSSoundStopper stopper = NBSSoundStopper.FromNote(0, note, 0);
            if (stopper.startLayer == 0)
                return LocalizationUtility.GetTextOrKey("runios-editor:inspector.nbs_player.note_info.stopper_all");

            return stopper.endLayer < stopper.startLayer ? LocalizationUtility.GetTextOrKey("runios-editor:inspector.nbs_player.note_info.stopper_off") : $"{stopper.startLayer}-{stopper.endLayer}";
        }

        /// <summary>
        /// Adds up to <c>0.5</c> to the alpha of the block, like a note that has just played in Note Block Studio. The alpha never exceeds <c>1</c>.<br/>
        /// 방금 재생된 Note Block Studio의 노트처럼 블록의 알파에 최대 <c>0.5</c>를 더합니다. 알파는 <c>1</c>을 넘지 않습니다.
        /// </summary>
        /// <param name="amount">
        /// <c>0</c> shows the alpha of the note and <c>1</c> adds the whole <c>0.5</c>.<br/>
        /// <c>0</c>이면 노트의 알파를 표시하고, <c>1</c>이면 <c>0.5</c>를 모두 더합니다.
        /// </param>
        public void SetFlash(float amount) => style.unityBackgroundImageTintColor = new Color(1, 1, 1, Math.Min(1, baseAlpha + (0.5f * amount)));

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

#nullable enable
using System.Globalization;

namespace RuniOS.NBS
{
    /// <summary>
    /// Stores one custom instrument definition.<br/>
    /// 커스텀 악기 정의 하나를 저장합니다.
    /// </summary>
    /// <param name="name">
    /// The display name. Note Block Studio derives <see cref="function"/> from it.<br/>
    /// 표시 이름입니다. Note Block Studio는 이 이름으로 <see cref="function"/>을 정합니다.
    /// </param>
    /// <param name="soundFile">
    /// The sound file path relative to the Note Block Studio <c>Sounds</c> folder.<br/>
    /// Note Block Studio <c>Sounds</c> 폴더 기준의 사운드 파일 경로입니다.
    /// </param>
    /// <param name="key">
    /// The key at which the sound plays at its original pitch. <c>45</c> is F#4.<br/>
    /// 사운드가 원래 피치로 재생되는 건반입니다. <c>45</c>는 F#4입니다.
    /// </param>
    /// <param name="pressKey">
    /// Whether the editor piano presses keys for this instrument.<br/>
    /// 에디터 피아노가 이 악기의 건반을 누를지 여부입니다.
    /// </param>
    [Serializable]
    public record struct NBSInstrument
    (
        [field: SerializeField] string name,
        [field: SerializeField] string soundFile,
        [field: SerializeField] byte key,
        [field: SerializeField] bool pressKey
    )
    {
        /// <summary>
        /// The number of built-in instruments that precede custom instruments in <see cref="NBSNote.instrument"/>.<br/>
        /// <see cref="NBSNote.instrument"/>에서 커스텀 악기 앞에 오는 기본 악기 수입니다.
        /// </summary>
        public const int vanillaCount = 20;

        const string changeColorPrefix = "Change Color to #";

        /// <summary>
        /// Gets the special behavior assigned by <see cref="name"/>.<br/>
        /// <see cref="name"/>으로 부여된 특수 동작을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The rules follow the Note Block Studio playback code.
        /// Names must match exactly without trimming, except that <see cref="NBSInstrumentFunction.changeColor"/> requires <c>Change Color to #</c> to appear exactly once, ignoring case.
        /// <br/><br/>
        /// 규칙은 Note Block Studio의 재생 코드를 따릅니다.
        /// 이름은 Trim 없이 정확히 일치해야 하며, <see cref="NBSInstrumentFunction.changeColor"/>만 대소문자를 무시하고 <c>Change Color to #</c>가 정확히 한 번 나타나야 합니다.
        /// </remarks>
        public readonly NBSInstrumentFunction function => name switch
        {
            "Tempo Changer" => NBSInstrumentFunction.tempoChanger,
            "Toggle Rainbow" => NBSInstrumentFunction.toggleRainbow,
            "Sound Stopper" => NBSInstrumentFunction.soundStopper,
            "Show Save Popup" => NBSInstrumentFunction.showSavePopup,
            _ when ContainsChangeColorPrefixOnce(name) => NBSInstrumentFunction.changeColor,
            "Toggle Background Accent" => NBSInstrumentFunction.toggleBackgroundAccent,
            _ => NBSInstrumentFunction.none
        };

        /// <summary>
        /// Tries to get the accent color of a <see cref="NBSInstrumentFunction.changeColor"/> instrument.<br/>
        /// <see cref="NBSInstrumentFunction.changeColor"/> 악기의 강조 색을 가져오려고 시도합니다.
        /// </summary>
        /// <param name="color">
        /// The parsed color when successful; otherwise, <see langword="default"/>.<br/>
        /// 성공하면 파싱된 색이고, 그렇지 않으면 <see langword="default"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the instrument changes the color and its hexadecimal digits are valid; otherwise, <see langword="false"/>.<br/>
        /// 악기가 색을 변경하고 16진수 자릿수가 유효하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <remarks>
        /// Like Note Block Studio, the digits are read at fixed character positions 17 to 22 from the start of the name.<br/>
        /// Note Block Studio와 같이 이름 시작 기준 고정 문자 위치 17~22에서 자릿수를 읽습니다.
        /// </remarks>
        public readonly bool TryGetColor(out Color32 color)
        {
            color = default;
            if (function != NBSInstrumentFunction.changeColor || name.Length < changeColorPrefix.Length + 6)
                return false;

            ReadOnlySpan<char> digits = name.AsSpan(changeColorPrefix.Length, 6);
            if (!byte.TryParse(digits[..2], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte r) ||
                !byte.TryParse(digits[2..4], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte g) ||
                !byte.TryParse(digits[4..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte b))
                return false;

            color = new Color32(r, g, b, 255);
            return true;
        }

        static bool ContainsChangeColorPrefixOnce(string name)
        {
            int index = name.IndexOf(changeColorPrefix, StringComparison.OrdinalIgnoreCase);
            return index >= 0 && name.IndexOf(changeColorPrefix, index + 1, StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}

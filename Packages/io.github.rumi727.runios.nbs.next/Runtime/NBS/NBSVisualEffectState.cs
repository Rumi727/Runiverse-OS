#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores the visual effect state that starts at a tick.<br/>
    /// 어떤 tick에서 시작하는 시각 효과 상태를 저장합니다.
    /// </summary>
    /// <param name="tick">
    /// The first tick where the state applies.<br/>
    /// 상태가 적용되는 첫 tick입니다.
    /// </param>
    /// <param name="rainbow">
    /// Whether the rainbow accent effect is on.<br/>
    /// 무지개 강조 효과가 켜져 있는지 여부입니다.
    /// </param>
    /// <param name="backgroundAccent">
    /// Whether the background accent effect is on.<br/>
    /// 배경 강조 효과가 켜져 있는지 여부입니다.
    /// </param>
    /// <param name="color">
    /// The last accent color set by <see cref="NBSInstrumentFunction.changeColor"/>, or <see langword="null"/> before the first one.<br/>
    /// <see cref="NBSInstrumentFunction.changeColor"/>가 마지막으로 설정한 강조 색이며, 첫 변경 이전이면 <see langword="null"/>입니다.
    /// </param>
    public readonly record struct NBSVisualEffectState(int tick, bool rainbow, bool backgroundAccent, Color32? color);
}

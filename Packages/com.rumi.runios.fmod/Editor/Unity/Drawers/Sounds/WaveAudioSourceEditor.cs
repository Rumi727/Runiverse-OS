#nullable enable
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaveAudioSource), true)]
    public class WaveAudioSourceEditor : RuniAudioSourceEditor<WaveAudioSource>
    {
        public WaveAudioSourceEditor() => playableController.timeUnits.Add(new WaveAudioTimeUnit());
    }
}
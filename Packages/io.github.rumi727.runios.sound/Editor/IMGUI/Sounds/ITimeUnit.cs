#nullable enable
using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public interface ITimeUnit
    {
        public void DrawField(Rect position, IEnumerable<IAudioPlayer> values);
        public float GetHeight();

        public string TimeToString(IAudioPlayer? value);
        public string RemainingTimeToString(IAudioPlayer? value);
        public string LengthToString(IAudioPlayer? value);
    }
}
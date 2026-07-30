#nullable enable
using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public interface ITimeUnit
    {
        public void DrawField(Rect position, IReadOnlyList<IPlayable> playables);
        public float GetHeight();

        public string TimeToString(IPlayable? playable);
        public string RemainingTimeToString(IPlayable? playable);
        public string LengthToString(IPlayable? playable);
    }
}
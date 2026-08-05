#nullable enable
using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public interface ITimeUnit<in T> : ITimeUnit where T : IAudioPlayer
    {
        public void DrawField(Rect position, IEnumerable<T> playables);
        void ITimeUnit.DrawField(Rect position, IEnumerable<IAudioPlayer> values) => DrawField(position, values.OfType<T>());

        public string TimeToString(T? playable);
        string ITimeUnit.TimeToString(IAudioPlayer? value) => TimeToString((T?)value);

        public string RemainingTimeToString(T? playable);
        string ITimeUnit.RemainingTimeToString(IAudioPlayer? value) => RemainingTimeToString((T?)value);

        public string LengthToString(T? playable);
        string ITimeUnit.LengthToString(IAudioPlayer? value) => LengthToString((T?)value);
    }
}
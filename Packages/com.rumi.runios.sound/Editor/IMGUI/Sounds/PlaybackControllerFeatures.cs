namespace RuniOS.Editor.IMGUI.Sounds
{
    [Flags]
    public enum PlaybackControllerFeatures
    {
        none = 0,
        pause = 1 << 1,
        timeline = 1 << 2,
        skip = 1 << 3,
        loopRange = 1 << 4,
        all = pause | timeline | skip | loopRange
    }
}
namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public record struct DrawerContext(bool isInArray = false, Rect? clipping = null)
    {
        public bool isInArray = isInArray;
        public Rect? clipping = clipping;
    }
}
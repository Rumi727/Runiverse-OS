namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public readonly record struct DrawerContext(bool isInArray = false, Rect? clipping = null)
    {
        public DrawerContext(Rect? clipping = null) : this(false, clipping) { }

        public DrawerContext InArray() => this with { isInArray = true };

        public static DrawerContext NewInArray() => new DrawerContext(true);
    }
}
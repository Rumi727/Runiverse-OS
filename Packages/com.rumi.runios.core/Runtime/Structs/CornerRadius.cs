namespace RuniOS
{
    [Serializable]
    public struct CornerRadius
    {
        public CornerRadius(float radius)
        {
            bottomLeft = radius;
            topLeft = radius;
            topRight = radius;
            bottomRight = radius;
        }
        
        public CornerRadius(float bottomLeft, float topLeft, float topRight, float bottomRight)
        {
            this.bottomLeft = bottomLeft;
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
        }

        [FieldName("gui.bottom_left")] public float bottomLeft;
        [FieldName("gui.top_left")] public float topLeft;
        [FieldName("gui.top_right")] public float topRight;
        [FieldName("gui.bottom_right")] public float bottomRight;
        
        public static implicit operator CornerRadius(float radius) => new CornerRadius(radius);
    }
}
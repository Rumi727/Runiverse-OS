#nullable enable
using Newtonsoft.Json;

namespace RuniOS
{
    /// <summary>
    /// 직사각형을 구성하는 4개 꼭짓점(Vertices)의 좌표 정보를 정의하는 구조체입니다.
    /// 좌측 상단부터 시계 방향(TL, TR, BR, BL) 순서로 좌표를 저장합니다.
    /// </summary>
    [Serializable]
    public struct RectVertices : IEquatable<RectVertices>
    {
        /// <summary>
        /// 4개의 꼭짓점 좌표를 지정하여 <see cref="RectVertices"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="topLeft">좌측 상단 좌표</param>
        /// <param name="topRight">우측 상단 좌표</param>
        /// <param name="bottomRight">우측 하단 좌표</param>
        /// <param name="bottomLeft">좌측 하단 좌표</param>
        public RectVertices(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            this.bottomLeft = bottomLeft;
        }

        /// <summary>
        /// <see cref="Rect"/> 정보를 기반으로 <see cref="RectVertices"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        public RectVertices(Rect rect)
        {
            topLeft = new Vector2(rect.xMin, rect.yMax);
            topRight = new Vector2(rect.xMax, rect.yMax);
            bottomRight = new Vector2(rect.xMax, rect.yMin);
            bottomLeft = new Vector2(rect.xMin, rect.yMin);
        }

        /// <summary>
        /// 최소 좌표(Min)와 최대 좌표(Max)를 사용하여 초기화합니다.
        /// </summary>
        public RectVertices(Vector2 min, Vector2 max)
        {
            topLeft = new Vector2(min.x, max.y);
            topRight = new Vector2(max.x, max.y);
            bottomRight = new Vector2(max.x, min.y);
            bottomLeft = new Vector2(min.x, min.y);
        }

        /// <summary>
        /// 이 꼭짓점들을 포함하는 축 정렬 경계 상자(AABB)인 <see cref="Rect"/>를 가져오거나 설정합니다.
        /// </summary>
        [JsonIgnore]
        public Rect rect
        {
            readonly get => this; // 암시적 변환 연산자 사용
            set
            {
                Rect rect = value;
                topLeft = new Vector2(rect.xMin, rect.yMax);
                topRight = new Vector2(rect.xMax, rect.yMax);
                bottomRight = new Vector2(rect.xMax, rect.yMin);
                bottomLeft = new Vector2(rect.xMin, rect.yMin);
            }
        }

        /// <summary>
        /// 좌측 상단(Top-Left) 좌표입니다.
        /// </summary>
        [FieldName("gui.top_left")] public Vector2 topLeft;

        /// <summary>
        /// 우측 상단(Top-Right) 좌표입니다.
        /// </summary>
        [FieldName("gui.top_right")] public Vector2 topRight;

        /// <summary>
        /// 우측 하단(Bottom-Right) 좌표입니다.
        /// </summary>
        [FieldName("gui.bottom_right")] public Vector2 bottomRight;

        /// <summary>
        /// 좌측 하단(Bottom-Left) 좌표입니다.
        /// </summary>
        [FieldName("gui.bottom_left")] public Vector2 bottomLeft;


        /// <summary>
        /// <see cref="RectVertices"/>를 <see cref="Rect"/>로 암시적으로 변환합니다.
        /// (BottomLeft를 기준으로 크기를 계산하여 축 정렬 사각형을 만듭니다)
        /// </summary>
        public static implicit operator Rect(RectVertices value)
            => new Rect(value.bottomLeft, value.topRight - value.bottomLeft); // BottomLeft를 기준점(Pos)으로, TopRight와의 차이를 크기(Size)로 계산

        /// <summary>
        /// <see cref="Rect"/>를 <see cref="RectVertices"/>로 암시적으로 변환합니다.
        /// </summary>
        public static implicit operator RectVertices(Rect value) => new RectVertices(value);

        public static bool operator ==(RectVertices left, RectVertices right) => left.Equals(right);
        public static bool operator !=(RectVertices left, RectVertices right) => !(left == right);
        
        public override readonly bool Equals(object? obj) => obj is RectVertices value && Equals(value);
        
        public readonly bool Equals(RectVertices other)
        {
            return topLeft.Equals(other.topLeft) &&
                   topRight.Equals(other.topRight) &&
                   bottomRight.Equals(other.bottomRight) &&
                   bottomLeft.Equals(other.bottomLeft);
        }
        
        public override readonly int GetHashCode() => HashCode.Combine(topLeft, topRight, bottomRight, bottomLeft);
    }
}
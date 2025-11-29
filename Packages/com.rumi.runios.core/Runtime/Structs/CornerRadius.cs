namespace RuniOS
{
    /// <summary>
    /// 직사각형의 각 모서리(Corner) 반경을 정의하는 구조체입니다.
    /// 좌측 하단, 좌측 상단, 우측 상단, 우측 하단 모서리의 반지름 값을 개별적으로 저장합니다.
    /// </summary>
    [Serializable]
    public struct CornerRadius : IEquatable<CornerRadius>, IEquatable<float>
    {
        /// <summary>
        /// 모든 모서리에 동일한 반경 값을 사용하여 <see cref="CornerRadius"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="radius">모든 모서리에 적용할 반경 값입니다.</param>
        public CornerRadius(float radius)
        {
            bottomLeft = radius;
            topLeft = radius;
            topRight = radius;
            bottomRight = radius;
        }
        
        /// <summary>
        /// 각 모서리에 대해 개별적인 반경 값을 사용하여 <see cref="CornerRadius"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="topLeft">좌측 상단 모서리의 반경입니다.</param>
        /// <param name="topRight">우측 상단 모서리의 반경입니다.</param>
        /// <param name="bottomRight">우측 하단 모서리의 반경입니다.</param>
        /// <param name="bottomLeft">좌측 하단 모서리의 반경입니다.</param>
        public CornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            this.bottomLeft = bottomLeft;
        }

        /// <summary>
        /// 좌측 상단(Top-Left) 모서리의 반경입니다.
        /// </summary>
        [FieldName("gui.top_left"), Min(0)] public float topLeft;
        
        /// <summary>
        /// 우측 상단(Top-Right) 모서리의 반경입니다.
        /// </summary>
        [FieldName("gui.top_right"), Min(0)] public float topRight;
        
        /// <summary>
        /// 우측 하단(Bottom-Right) 모서리의 반경입니다.
        /// </summary>
        [FieldName("gui.bottom_right"), Min(0)] public float bottomRight;
        
        /// <summary>
        /// 좌측 하단(Bottom-Left) 모서리의 반경입니다.
        /// </summary>
        [FieldName("gui.bottom_left"), Min(0)] public float bottomLeft;
        
        /// <summary>
        /// 현재 객체가 다른 <see cref="CornerRadius"/> 객체와 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="other">이 객체와 비교할 객체입니다.</param>
        /// <returns>현재 객체가 <paramref name="other"/> 매개 변수와 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool Equals(CornerRadius other) => topLeft.Equals(other.topLeft) && topRight.Equals(other.topRight) && bottomRight.Equals(other.bottomRight) && bottomLeft.Equals(other.bottomLeft);

        /// <summary>
        /// 현재 객체의 모든 모서리 반경이 주어진 <see cref="float"/> 값과 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="other">비교할 단일 반경 값입니다.</param>
        /// <returns>모든 모서리의 반경이 <paramref name="other"/>와 같으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool Equals(float other) => Equals((CornerRadius)other);
        
        /// <summary>
        /// 지정된 객체가 현재 객체와 같은지 여부를 확인합니다.
        /// </summary>
        /// <param name="obj">비교할 객체입니다.</param>
        /// <returns>지정된 객체가 <see cref="CornerRadius"/> 또는 <see cref="float"/>이며 현재 객체와 같으면 <see langword="true"/>입니다.</returns>
        public override bool Equals(object? obj) => obj switch
        {
            CornerRadius other => Equals(other),
            float other => Equals(other),
            _ => false
        };

        /// <summary>
        /// 이 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>이 인스턴스의 해시 코드인 32비트 부호 있는 정수입니다.</returns>
        public override int GetHashCode() => HashCode.Combine(topLeft, topRight, bottomRight, bottomLeft);
        
        /// <summary>
        /// 두 <see cref="CornerRadius"/> 인스턴스가 같은지 비교합니다.
        /// </summary>
        public static bool operator ==(CornerRadius left, CornerRadius right) => left.Equals(right);

        /// <summary>
        /// 두 <see cref="CornerRadius"/> 인스턴스가 다른지 비교합니다.
        /// </summary>
        public static bool operator !=(CornerRadius left, CornerRadius right) => !left.Equals(right);
        
        /// <summary>
        /// <see cref="CornerRadius"/> 인스턴스와 <see cref="float"/> 값이 같은지 비교합니다.
        /// </summary>
        public static bool operator ==(CornerRadius left, float right) => left.Equals(right);

        /// <summary>
        /// <see cref="CornerRadius"/> 인스턴스와 <see cref="float"/> 값이 다른지 비교합니다.
        /// </summary>
        public static bool operator !=(CornerRadius left, float right) => !left.Equals(right);
        
        /// <summary>
        /// <see cref="float"/> 값과 <see cref="CornerRadius"/> 인스턴스가 같은지 비교합니다.
        /// </summary>
        public static bool operator ==(float left, CornerRadius right) => ((CornerRadius)left).Equals(right);

        /// <summary>
        /// <see cref="float"/> 값과 <see cref="CornerRadius"/> 인스턴스가 다른지 비교합니다.
        /// </summary>
        public static bool operator !=(float left, CornerRadius right) => !((CornerRadius)left).Equals(right);
        
        /// <summary>
        /// <see cref="float"/> 값을 <see cref="CornerRadius"/>로 암시적으로 변환합니다. 
        /// 모든 모서리가 해당 값으로 설정됩니다.
        /// </summary>
        /// <param name="radius">모서리 반경 값입니다.</param>
        public static implicit operator CornerRadius(float radius) => new CornerRadius(radius);
    }
}
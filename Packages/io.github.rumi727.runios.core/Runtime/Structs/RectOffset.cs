#nullable enable
using Newtonsoft.Json;
using UnityEngine;

namespace RuniOS
{
    [Serializable]
    public struct RectOffset(float left, float right, float top, float bottom) : IEquatable<RectOffset>
    {
        public RectOffset(float value) : this(value, value, value, value) { }

        public RectOffset(Vector2 min, Vector2 max) : this(min.x, max.x, max.y, min.y) { }

        [FieldName("runios-editor:gui.left")] public float left = left;
        [FieldName("runios-editor:gui.right")] public float right = right;
        [FieldName("runios-editor:gui.top")] public float top = top;
        [FieldName("runios-editor:gui.bottom")] public float bottom = bottom;

        [JsonIgnore]
        public Vector2 min
        {
            readonly get => new Vector2(left, bottom);
            set
            {
                left = value.x;
                bottom = value.y;
            }
        }

        [JsonIgnore]
        public Vector2 max
        {
            readonly get => new Vector2(right, top);
            set
            {
                right = value.x;
                top = value.y;
            }
        }

        /// <summary>
        /// Gets the sum of <see cref="left"/> and <see cref="right"/>.<br/>
        /// <see cref="left"/>와 <see cref="right"/>의 합을 가져옵니다.
        /// </summary>
        [JsonIgnore] public readonly float horizontal => left + right;

        /// <summary>
        /// Gets the sum of <see cref="top"/> and <see cref="bottom"/>.<br/>
        /// <see cref="top"/>과 <see cref="bottom"/>의 합을 가져옵니다.
        /// </summary>
        [JsonIgnore] public readonly float vertical => top + bottom;

        [JsonIgnore] public static RectOffset zero => new RectOffset(Vector2.zero, Vector2.zero);

        /// <summary>
        /// Returns <paramref name="rect"/> grown outward by this offset.<br/>
        /// 이 오프셋만큼 바깥으로 넓힌 <paramref name="rect"/>를 반환합니다.
        /// </summary>
        /// <remarks>
        /// Like <see cref="RectTransform"/>, y grows upward, so <see cref="bottom"/> is applied to <see cref="Rect.y"/>. This differs from <see cref="UnityEngine.RectOffset.Add"/>, which applies <see cref="UnityEngine.RectOffset.top"/> to it.<br/>
        /// <see cref="RectTransform"/>처럼 y가 위로 커지므로 <see cref="bottom"/>을 <see cref="Rect.y"/>에 적용합니다. <see cref="UnityEngine.RectOffset.top"/>을 적용하는 <see cref="UnityEngine.RectOffset.Add"/>와 다릅니다.
        /// </remarks>
        /// <param name="rect">
        /// The rectangle to grow.<br/>
        /// 넓힐 사각형입니다.
        /// </param>
        /// <returns>
        /// The grown rectangle.<br/>
        /// 넓힌 사각형을 반환합니다.
        /// </returns>
        public readonly Rect Add(Rect rect) => new Rect(rect.x - left, rect.y - bottom, rect.width + horizontal, rect.height + vertical);

        /// <summary>
        /// Returns <paramref name="rect"/> shrunk inward by this offset.<br/>
        /// 이 오프셋만큼 안쪽으로 줄인 <paramref name="rect"/>를 반환합니다.
        /// </summary>
        /// <remarks>
        /// Like <see cref="RectTransform"/>, y grows upward, so <see cref="bottom"/> is applied to <see cref="Rect.y"/>. This differs from <see cref="UnityEngine.RectOffset.Remove"/>, which applies <see cref="UnityEngine.RectOffset.top"/> to it.<br/>
        /// <see cref="RectTransform"/>처럼 y가 위로 커지므로 <see cref="bottom"/>을 <see cref="Rect.y"/>에 적용합니다. <see cref="UnityEngine.RectOffset.top"/>을 적용하는 <see cref="UnityEngine.RectOffset.Remove"/>와 다릅니다.
        /// </remarks>
        /// <param name="rect">
        /// The rectangle to shrink.<br/>
        /// 줄일 사각형입니다.
        /// </param>
        /// <returns>
        /// The shrunk rectangle.<br/>
        /// 줄인 사각형을 반환합니다.
        /// </returns>
        public readonly Rect Remove(Rect rect) => new Rect(rect.x + left, rect.y + bottom, rect.width - horizontal, rect.height - vertical);



        public static bool operator ==(RectOffset left, RectOffset right) => left.Equals(right);
        public static bool operator !=(RectOffset left, RectOffset right) => !(left == right);



        public static implicit operator RectOffset(UnityEngine.RectOffset v)
        {
            RectOffset result = new RectOffset
            {
                left = v.left,
                right = v.right,
                top = v.top,
                bottom = v.bottom
            };

            return result;
        }

        public static implicit operator UnityEngine.RectOffset(RectOffset v)
        {
            UnityEngine.RectOffset result = new UnityEngine.RectOffset
            {
                left = (int)v.left,
                right = (int)v.right,
                top = (int)v.top,
                bottom = (int)v.bottom
            };

            return result;
        }



        public readonly bool Equals(RectOffset other) => left.Equals(other.left) && right.Equals(other.right) && top.Equals(other.top) && bottom.Equals(other.bottom);
        public override readonly bool Equals(object? obj) => obj is RectOffset result && Equals(result);

        public override readonly int GetHashCode() => HashCode.Combine(left, right, top, bottom);

        public override readonly string ToString() => $"RectOffset l:{left} r:{right} t:{top} b:{bottom}";
    }
}
#nullable enable
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    public struct RectCorner : IEquatable<RectCorner>
    {
        public RectCorner(Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight)
        {
            this.bottomLeft = bottomLeft;
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
        }

        public RectCorner(Rect rect)
        {
            bottomLeft = new Vector2(rect.xMin, rect.yMin);
            topLeft = new Vector2(rect.xMin, rect.yMax);
            topRight = new Vector2(rect.xMax, rect.yMax);
            bottomRight = new Vector2(rect.xMax, rect.yMin);
        }

        public RectCorner(Vector2 min, Vector2 max)
        {
            bottomLeft = new Vector2(min.x, min.y);
            topLeft = new Vector2(min.x, max.y);
            topRight = new Vector2(max.x, max.y);
            bottomRight = new Vector2(max.x, min.y);
        }



        [JsonIgnore]
        public Rect rect
        {
            readonly get => this;
            set
            {
                Rect rect = value;

                bottomLeft = new Vector2(rect.xMin, rect.yMin);
                topLeft = new Vector2(rect.xMin, rect.yMax);
                topRight = new Vector2(rect.xMax, rect.yMax);
                bottomRight = new Vector2(rect.xMax, rect.yMin);
            }
        }

        [FieldName("gui.bottom_left")] public Vector2 bottomLeft;
        [FieldName("gui.top_left")] public Vector2 topLeft;
        [FieldName("gui.top_right")] public Vector2 topRight;
        [FieldName("gui.bottom_right")] public Vector2 bottomRight;



        public static implicit operator Rect(RectCorner value) => new Rect(value.bottomLeft, value.topRight - value.bottomLeft);
        public static implicit operator RectCorner(Rect value) => new RectCorner(value);

        public static bool operator ==(RectCorner left, RectCorner right) => left.Equals(right);
        public static bool operator !=(RectCorner left, RectCorner right) => !(left == right);



        public override readonly bool Equals(object? obj) => obj is RectCorner value && Equals(value);

        public readonly bool Equals(RectCorner other)
        {
            return bottomLeft.Equals(other.bottomLeft) &&
                   topLeft.Equals(other.topLeft) &&
                   topRight.Equals(other.topRight) &&
                   bottomRight.Equals(other.bottomRight);
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = 816633021;
                hash *= 122684447 + bottomLeft.GetHashCode();
                hash *= 245107901 + topLeft.GetHashCode();
                hash *= -619998250 + topRight.GetHashCode();
                hash *= 499623778 + bottomRight.GetHashCode();

                return hash;
            }
        }
    }
}

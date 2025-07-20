#nullable enable
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    public struct RectOffset : IEquatable<RectOffset>
    {
        public RectOffset(float value) : this(value, value, value, value) { }

        public RectOffset(Vector2 min, Vector2 max) : this(min.x, max.x, max.y, min.y) { }

        public RectOffset(float left, float right, float top, float bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        [FieldName("gui.left")] public float left;
        [FieldName("gui.right")] public float right;
        [FieldName("gui.top")] public float top;
        [FieldName("gui.bottom")] public float bottom;

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

        [JsonIgnore] public static RectOffset zero => new RectOffset(Vector2.zero, Vector2.zero);



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

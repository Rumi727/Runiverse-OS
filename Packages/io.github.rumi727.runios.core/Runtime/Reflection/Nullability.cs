#nullable enable
using System.Collections.Immutable;
using System.Reflection;

namespace RuniOS.Reflection
{
    /// <summary>
    /// Describes the nullability of a type usage, including the nullability of its element type or generic type arguments.<br/>
    /// Created from a reflected member through the <c>Create</c> methods, which combine the .NET <see cref="NullabilityInfoContext"/> with <see cref="NullableType"/> so that custom nullable value types such as <see cref="SerializableNullable{T}"/> are reported as nullable.
    /// <br/><br/>
    /// 타입 사용 지점의 nullable 여부를 요소 타입과 제네릭 타입 인자까지 포함해 나타냅니다.<br/>
    /// <c>Create</c> 메서드로 리플렉션 멤버에서 만들며, .NET의 <see cref="NullabilityInfoContext"/>와 <see cref="NullableType"/>을 결합해 <see cref="SerializableNullable{T}"/> 같은 사용자 정의 nullable 값 타입도 nullable로 보고합니다.
    /// </summary>
    /// <param name="type">
    /// The type this nullability describes.<br/>
    /// 이 nullability가 설명하는 타입입니다.
    /// </param>
    /// <param name="readState">
    /// The nullability when the value is read.<br/>
    /// 값을 읽을 때의 nullable 상태입니다.
    /// </param>
    /// <param name="writeState">
    /// The nullability when the value is written.<br/>
    /// 값을 쓸 때의 nullable 상태입니다.
    /// </param>
    /// <param name="children">
    /// The nullability of the element type for arrays, or of the generic type arguments for generic types.<br/>
    /// 배열이면 요소 타입, 제네릭 타입이면 제네릭 타입 인자의 nullability입니다.
    /// </param>
    public readonly record struct Nullability(Type type, NullState readState, NullState writeState, ImmutableArray<Nullability> children)
    {
        /// <summary>
        /// Gets the nullability of the array element type, or <see langword="null"/> if <see cref="type"/> is not an array.<br/>
        /// 배열의 요소 타입 nullability를 가져오며, <see cref="type"/>이 배열이 아니면 <see langword="null"/>입니다.
        /// </summary>
        public Nullability? elementType => type.IsArray && !children.IsDefaultOrEmpty ? children[0] : null;

        /// <summary>
        /// Gets the nullability of the generic type arguments, or an empty array if <see cref="type"/> has none.<br/>
        /// 제네릭 타입 인자의 nullability를 가져오며, 없으면 빈 배열입니다.
        /// </summary>
        public ImmutableArray<Nullability> genericTypeArguments => type.IsArray || children.IsDefault ? [] : children;

        /// <summary>
        /// Gets the nullability of the generic type argument at <paramref name="index"/>, or <see langword="null"/> if there is none.<br/>
        /// <paramref name="index"/>번째 제네릭 타입 인자의 nullability를 가져오며, 없으면 <see langword="null"/>입니다.
        /// </summary>
        public Nullability? GetGenericTypeArgument(int index)
        {
            ImmutableArray<Nullability> arguments = genericTypeArguments;
            return (uint)index < (uint)arguments.Length ? arguments[index] : null;
        }

        /// <summary>
        /// Creates the nullability of the specified parameter.<br/>
        /// 지정한 매개변수의 nullability를 만듭니다.
        /// </summary>
        public static Nullability Create(ParameterInfo parameterInfo) => From(new NullabilityInfoContext().Create(parameterInfo));

        /// <summary>
        /// Creates the nullability of the specified property.<br/>
        /// 지정한 프로퍼티의 nullability를 만듭니다.
        /// </summary>
        public static Nullability Create(PropertyInfo propertyInfo) => From(new NullabilityInfoContext().Create(propertyInfo));

        /// <summary>
        /// Creates the nullability of the specified field.<br/>
        /// 지정한 필드의 nullability를 만듭니다.
        /// </summary>
        public static Nullability Create(FieldInfo fieldInfo) => From(new NullabilityInfoContext().Create(fieldInfo));

        /// <summary>
        /// Creates the nullability of the specified event.<br/>
        /// 지정한 이벤트의 nullability를 만듭니다.
        /// </summary>
        public static Nullability Create(EventInfo eventInfo) => From(new NullabilityInfoContext().Create(eventInfo));

        /// <summary>
        /// Determines whether two nullabilities describe the same type with the same states and children.<br/>
        /// 두 nullability가 같은 타입, 같은 상태, 같은 하위 항목을 가리키는지 확인합니다.
        /// </summary>
        public bool Equals(Nullability other) => type == other.type && readState == other.readState && writeState == other.writeState && children.AsSpan().SequenceEqual(other.children.AsSpan());

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(type);
            hash.Add(readState);
            hash.Add(writeState);

            foreach (Nullability child in children.AsSpan())
                hash.Add(child);

            return hash.ToHashCode();
        }

        static Nullability From(System.Reflection.NullabilityInfo info)
        {
            NullState readState = ToNullState(info.ReadState);
            NullState writeState = ToNullState(info.WriteState);

            // .NET은 SerializableNullable<T> 같은 사용자 정의 nullable 값 타입을 NotNull로 보고하므로 NullableType 기준으로 바로잡습니다.
            if (info.Type.IsValueType && info.Type.GetNullableUnderlyingType() != null)
                readState = writeState = NullState.Nullable;

            ImmutableArray<Nullability>.Builder children = ImmutableArray.CreateBuilder<Nullability>();
            if (info.ElementType is { } elementType)
                children.Add(From(elementType));
            else
            {
                foreach (System.Reflection.NullabilityInfo argument in info.GenericTypeArguments)
                    children.Add(From(argument));
            }

            return new Nullability(info.Type, readState, writeState, children.ToImmutable());
        }

        static NullState ToNullState(NullabilityState state) => state switch
        {
            NullabilityState.NotNull => NullState.NotNull,
            NullabilityState.Nullable => NullState.Nullable,
            _ => NullState.Unknown
        };
    }
}

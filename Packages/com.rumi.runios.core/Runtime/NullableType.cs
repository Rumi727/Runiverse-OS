#nullable enable
using System;
using System.Linq;

namespace RuniOS
{
    /// <summary>
    /// Handles the registration and checking of nullable types.
    /// <br/>
    /// Nullable 타입의 등록 및 확인을 처리합니다.
    /// </summary>
    public static class NullableType
    {
        /// <summary>
        /// A delegate that determines if a given <see cref="Type"/> is nullable.
        /// <br/>
        /// 주어진 <see cref="Type"/>이 nullable인지 여부를 결정하는 델리게이트입니다.
        /// </summary>
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public static event Func<Type, bool> isNullable;
        public static bool IsNullable(this Type type) => isNullable.GetInvocationList()
            .OfType<Func<Type, bool>>()
            .Any(x => x.Invoke(type));

        /// <summary>
        /// A delegate that retrieves the underlying type of a nullable <see cref="Type"/>.
        /// <br/>If the provided <see cref="Type"/> is a <see cref="Nullable{T}"/>, this delegate returns its generic type argument (T).
        /// <br/>If the provided <see cref="Type"/> is not a <see cref="Nullable{T}"/>, it returns <see langword="null"/>.
        /// <br/><br/>
        /// Nullable <see cref="Type"/>의 기본 타입을 검색하는 델리게이트입니다.
        /// <br/>제공된 <see cref="Type"/>이 <see cref="Nullable{T}"/>인 경우, 이 델리게이트는 해당 제네릭 타입 인자(T)를 반환합니다.
        /// <br/>제공된 <see cref="Type"/>이 <see cref="Nullable{T}"/>가 아닌 경우, <see langword="null"/>을 반환합니다.
        /// </summary>
        public static event Func<Type, Type?> getNullableUnderlyingType;
        public static Type? GetNullableUnderlyingType(this Type type) => getNullableUnderlyingType.GetInvocationList()
            .OfType<Func<Type, Type?>>()
            .Select(x => x.Invoke(type))
            .FirstOrDefault(x => x != null);
        
        /// <summary>
        /// A delegate that determines whether a given object is null.
        /// <br/>
        /// 주어진 오브젝트가 null 값인지 여부를 결정하는 델리게이트입니다.
        /// </summary>
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public static event Func<object?, bool> isNull;
        
        /// <summary>
        /// 주어진 오브젝트가 null 값인지 여부를 반환합니다.<br/>
        /// <see cref="Object.Equals(object)"/> 메소드를 사용하여 <see cref="SerializableNullable{T}"/> 및 <see cref="UnityEngine.Object"/> 등을 지원합니다.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNull(this object? value) => isNull.GetInvocationList()
            .OfType<Func<object?, bool>>()
            .Any(x => x.Invoke(value));

        /// <summary>
        /// Initializes the <see cref="NullableType"/> class.
        /// <br/>The <see cref="isNullable"/> delegate is populated to check for class types, interface types, and <see cref="Nullable{T}"/> types.
        /// <br/>The <see cref="getNullableUnderlyingType"/> delegate is populated to retrieve the underlying type of a nullable type.
        /// <br/><br/>
        /// <see cref="NullableType"/> 클래스를 초기화합니다.
        /// <br/><see cref="isNullable"/> 델리게이트는 클래스 타입, 인터페이스 타입 및 <see cref="Nullable{T}"/> 타입을 확인하도록 채워집니다.
        /// <br/><see cref="getNullableUnderlyingType"/> 델리게이트는 nullable 타입의 기본 타입을 검색하도록 채워집니다.
        /// </summary>
        static NullableType()
        {
            isNullable += x => x.IsClass || x.IsInterface || Nullable.GetUnderlyingType(x) != null || SerializableNullable.GetUnderlyingType(x) != null;
            
            getNullableUnderlyingType += Nullable.GetUnderlyingType;
            getNullableUnderlyingType += SerializableNullable.GetUnderlyingType;

            isNull += x => x == null || x.Equals(null);
        }
    }
}
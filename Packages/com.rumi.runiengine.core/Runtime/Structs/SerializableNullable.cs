#nullable enable
using System;

namespace RuniEngine
{
    /// <summary>
    /// <see cref="SerializableNullable{T}"/> 및 <see cref="Nullable{T}"/> 타입과 관련된 유틸리티 메서드 및 상수를 제공하는 정적 클래스입니다.
    /// </summary>
    public static class SerializableNullable
    {
        /// <summary>
        /// <see cref="SerializableNullable{T}"/> 구조체의 내부 값 필드 이름입니다.
        /// </summary>
        public const string nameofValue = "value";
        /// <summary>
        /// <see cref="SerializableNullable{T}"/> 구조체의 값 존재 여부 필드 이름입니다.
        /// </summary>
        public const string nameofHasValue = "hasValue";
        
        /// <summary>
        /// 지정된 nullable 타입의 기본 (<see cref="Nullable{T}"/> 또는 <see cref="ISerializableNullable{T}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="nullableType">기본 타입을 가져올 nullable 타입입니다 (예: <c>int?</c> 또는 <c>SerializableNullable&lt;int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="nullableType"/>이 <see cref="Nullable{T}"/> 또는 <see cref="ISerializableNullable{T}"/>의 인스턴스이면 해당 <typeparamref name="T"/> 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static Type? GetUnderlyingType(Type nullableType)
        {
            {
                // 표준 Nullable<T>의 기본 타입을 확인합니다.
                Type? resultType = Nullable.GetUnderlyingType(nullableType);
                if (resultType != null)
                    return resultType;
            }

            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (nullableType.IsGenericType && !nullableType.IsGenericTypeDefinition)
            {
                // ISerializableNullable<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (nullableType.IsAssignableToGenericDefinition(typeof(ISerializableNullable<>)))
                    return nullableType.GetGenericArguments()[0]; // 제네릭 인자(T)를 반환합니다.
            }

            return null;
        }
    }
}
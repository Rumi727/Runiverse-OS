#nullable enable
using System;

namespace RuniOS.Collections.Generic
{
    public static class SerializableDictionary
    {
        /// <summary>
        /// 지정된 딕셔너리 타입의 기본 (<see cref="ISerializableDictionary{TKey,TValue,TPair}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">기본 타입을 가져올 딕셔너리 타입입니다 (예: <c>SerializableDictionary&lt;string, int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="ISerializableDictionary{TKey,TValue,TPair}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static (Type? key, Type? value, Type? pair) GetUnderlyingType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // ISerializableNullable<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(ISerializableDictionary<,,>), out Type? resolvedType))
                {
                    Type key = resolvedType.GetGenericArguments()[0];
                    Type value = resolvedType.GetGenericArguments()[1];
                    Type pair = resolvedType.GetGenericArguments()[2];
                    
                    return (key, value, pair);
                }
            }

            return default;
        }
    }
}

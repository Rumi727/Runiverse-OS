#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace RuniOS.Collections.Generic
{
    public static class CollectionGenericUtility
    {
        /// <summary>
        /// 지정된 열거 가능한 타입의 요소 (<see cref="IEnumerable{T}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <returns>
        /// <paramref name="source"/>이 <see cref="IEnumerable{T}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see cref="object"/>입니다.
        /// </returns>
        public static Type GetElementType(this IEnumerable source) => GetEnumerableElementType(source.GetType()) ?? typeof(object);
        
        /// <summary>
        /// 지정된 열거 가능한 타입의 요소 (<see cref="IEnumerable{T}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">요소 타입을 가져올 열거 가능한 타입입니다 (예: <c>List&lt;int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="IEnumerable{T}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static Type? GetEnumerableElementType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // IEnumerable<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(IEnumerable<>), out Type? resolvedType))
                    return resolvedType.GetGenericArguments()[0];
            }

            return null;
        }
        
        /// <summary>
        /// 지정된 컬렉션 타입의 요소 (<see cref="ICollection{T}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">요소 타입을 가져올 컬렉션 타입입니다 (예: <c>List&lt;int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="ICollection{T}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static Type? GetCollectionElementType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // ICollection<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(ICollection<>), out Type? resolvedType))
                    return resolvedType.GetGenericArguments()[0];
            }

            return null;
        }
        
        /// <summary>
        /// 지정된 읽기 전용 컬렉션 타입의 요소 (<see cref="IReadOnlyCollection{T}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">요소 타입을 가져올 리스트 타입입니다 (예: <c>List&lt;int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="IReadOnlyCollection{T}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static Type? GetReadOnlyCollectionElementType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // ICollection<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(IReadOnlyCollection<>), out Type? resolvedType))
                    return resolvedType.GetGenericArguments()[0];
            }

            return null;
        }
        
        /// <summary>
        /// 지정된 리스트 타입의 요소 (<see cref="IList{T}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">요소 타입을 가져올 리스트 타입입니다 (예: <c>List&lt;int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="IList{T}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static Type? GetListElementType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // IList<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(IList<>), out Type? resolvedType))
                    return resolvedType.GetGenericArguments()[0];
            }

            return null;
        }
        
        /// <summary>
        /// 지정된 리스트 타입의 요소 (<see cref="IReadOnlyList{T}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">요소 타입을 가져올 리스트 타입입니다 (예: <c>List&lt;int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="IReadOnlyList{T}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static Type? GetReadOnlyListElementType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // IList<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(IReadOnlyList<>), out Type? resolvedType))
                    return resolvedType.GetGenericArguments()[0];
            }

            return null;
        }
        
        /// <summary>
        /// 지정된 딕셔너리 타입의 요소 (<see cref="IDictionary{TKey,TValue}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">요소 타입을 가져올 딕셔너리 타입입니다 (예: <c>Dictionary&lt;string, int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="IDictionary{TKey,TValue}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static KeyValuePair<Type, Type>? GetDictionaryElementType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // IDictionary<,>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(IDictionary<,>), out Type? resolvedType))
                {
                    Type key = resolvedType.GetGenericArguments()[0];
                    Type value = resolvedType.GetGenericArguments()[1];
                    
                    return new KeyValuePair<Type, Type>(key, value);
                }
            }

            return null;
        }
        
        /// <summary>
        /// 지정된 키-값 쌍 타입의 요소 (<see cref="KeyValuePair{TKey,TValue}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">요소 타입을 가져올 키-값 쌍 타입입니다 (예: <c>KeyValuePair&lt;string, int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="KeyValuePair{TKey,TValue}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static KeyValuePair<Type, Type>? GetKeyValuePairUnderlyingType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // KeyValuePair<,>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(KeyValuePair<,>), out Type? resolvedType))
                {
                    Type key = resolvedType.GetGenericArguments()[0];
                    Type value = resolvedType.GetGenericArguments()[1];
                    
                    return new KeyValuePair<Type, Type>(key, value);
                }
            }

            return null;
        }
    }
}
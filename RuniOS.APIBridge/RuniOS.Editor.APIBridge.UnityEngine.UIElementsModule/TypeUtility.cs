using System;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    static class TypeUtility
    {
        /// <summary>
        /// 주어진 <paramref name="givenType"/>이 특정 제네릭 타입 정의(<paramref name="genericTypeDefinition"/>)를
        /// 구현하거나 상속하는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 <paramref name="givenType"/>의 인터페이스 및 상속 계층 구조를 탐색하여
        /// <paramref name="genericTypeDefinition"/>과 일치하는 제네릭 타입 정의가 있는지 검사합니다.<br/>
        /// 예를 들어, <c>List&lt;int&gt;</c>가 <c>IEnumerable&lt;&gt;</c>를 구현하는지,
        /// 또는 <c>MyDerivedClass&lt;T&gt;</c>가 <c>BaseClass&lt;&gt;</c>로부터 파생되었는지 등을 확인할 수 있습니다.
        /// </remarks>
        /// <param name="givenType">확인할 대상 <see cref="Type"/>입니다.</param>
        /// <param name="genericTypeDefinition">찾으려는 제네릭 타입 정의입니다 (예: <c>typeof(List&lt;&gt;)</c>, <c>typeof(IDictionary&lt;,&gt;)</c>).</param>
        /// <returns>
        /// <paramref name="givenType"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하면
        /// <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="givenType"/> 또는 <paramref name="genericTypeDefinition"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="genericTypeDefinition"/>이 유효한 제네릭 타입 정의가 아닌 경우 발생할 수 있습니다.
        /// </exception>
        public static bool IsAssignableToGenericDefinition(this Type givenType, Type genericTypeDefinition) => IsAssignableToGenericDefinition(givenType, genericTypeDefinition, out _);

        /// <summary>
        /// 주어진 <paramref name="givenType"/>이 특정 제네릭 타입 정의(<paramref name="genericTypeDefinition"/>)를
        /// 구현하거나 상속하는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 <paramref name="givenType"/>의 인터페이스 및 상속 계층 구조를 탐색하여
        /// <paramref name="genericTypeDefinition"/>과 일치하는 제네릭 타입 정의가 있는지 검사합니다.<br/>
        /// 예를 들어, <c>List&lt;int&gt;</c>가 <c>IEnumerable&lt;&gt;</c>를 구현하는지,
        /// 또는 <c>MyDerivedClass&lt;T&gt;</c>가 <c>BaseClass&lt;&gt;</c>로부터 파생되었는지 등을 확인할 수 있습니다.
        /// </remarks>
        /// <param name="givenType">확인할 대상 <see cref="Type"/>입니다.</param>
        /// <param name="genericTypeDefinition">찾으려는 제네릭 타입 정의입니다 (예: <c>typeof(List&lt;&gt;)</c>, <c>typeof(IDictionary&lt;,&gt;)</c>).</param>
        /// <param name="resolvedType">
        /// <paramref name="givenType"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하는 경우,
        /// 실제로 발견된 구체적인 제네릭 타입(예: <c>List&lt;int&gt;</c>)이 반환됩니다.
        /// 찾지 못한 경우 <see langword="null"/>이 반환됩니다.
        /// </param>
        /// <returns>
        /// <paramref name="givenType"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하면
        /// <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="givenType"/> 또는 <paramref name="genericTypeDefinition"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="genericTypeDefinition"/>이 유효한 제네릭 타입 정의가 아닌 경우 발생할 수 있습니다.
        /// </exception>
        public static bool IsAssignableToGenericDefinition(this Type givenType, Type genericTypeDefinition, [MaybeNullWhen(false)] out Type resolvedType)
        {
            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (givenType == null)
                throw new ArgumentNullException(nameof(givenType), "The given type cannot be null.");
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), "The generic type definition cannot be null.");
            else if (!genericTypeDefinition.IsGenericTypeDefinition)
                throw new ArgumentException("The provided genericTypeDefinition must be a valid generic type definition (e.g., typeof(List<>) or typeof(IDictionary<,>)).", nameof(genericTypeDefinition));
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

            Type? currentType = givenType;
            while (currentType != null)
            {
                // 인터페이스 확인
                var interfaceTypes = currentType.GetInterfaces();
                foreach (var it in interfaceTypes)
                {
                    if (it.IsGenericType && it.GetGenericTypeDefinition() == genericTypeDefinition)
                    {
                        resolvedType = it;
                        return true;
                    }
                }

                // 현재 타입 확인 (직접적인 제네릭 타입 정의 일치)
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == genericTypeDefinition)
                {
                    resolvedType = currentType;
                    return true;
                }

                // 기반 클래스 확인
                currentType = currentType.BaseType;
            }

            resolvedType = null;
            return false;
        }
    }
}
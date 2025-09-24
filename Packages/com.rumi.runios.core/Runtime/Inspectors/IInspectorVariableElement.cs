#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RuniOS.Inspectors
{
    /// <summary>
    /// 인스펙터에 표시되는 변수 요소를 정의하는 인터페이스입니다.
    /// </summary>
    public interface IInspectorVariableElement : IInspectorElement
    {
        /// <summary>
        /// 변수의 타입을 가져옵니다.
        /// </summary>
        Type variableType { get; }

        /// <summary>
        /// 변수의 null 허용 여부 정보를 가져옵니다.
        /// </summary>
        NullabilityInfo? nullabilityInfo { get; }

        /// <summary>
        /// 변수가 정적인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isStatic { get; }

        /// <summary>
        /// 변수를 읽을 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isReadable { get; }

        /// <summary>
        /// 변수에 쓸 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isWritable { get; }

        /// <summary>
        /// 변수의 값을 가져오거나 설정합니다.
        /// </summary>
        object? value { get; set; }

        /// <summary>
        /// 여러 객체를 검사할 때 값이 혼합되어 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isMixedValue { get; }

        /// <summary>
        /// 이 요소가 속한 <see cref="IInspectableObject"/>를 가져옵니다.
        /// </summary>
        /// <remarks>이 프로퍼티를 구현할 때 get 접근자는 내부적으로 인스턴스를 설정해야 합니다. (<see cref="Csharp.FieldElement.inspectableObjectElement"/> 참조) 따라서 이 프로퍼티를 사용하는 코드는 get 접근 시점에 인스턴스가 설정된다는 점을 참고해야 합니다.</remarks>
        IInspectableObject inspectableObjectElement { get; }

        /// <summary>
        /// 이 요소가 리스트의 일부인 경우 <see cref="IInspectableList"/>를 가져옵니다.
        /// </summary>
        /// <remarks>이 프로퍼티를 구현할 때 get 접근자는 내부적으로 인스턴스를 설정해야 합니다. (<see cref="Csharp.FieldElement.inspectableElementList"/> 참조) 따라서 이 프로퍼티를 사용하는 코드는 get 접근 시점에 인스턴스가 설정된다는 점을 참고해야 합니다.</remarks>
        IInspectableList? inspectableListElement { get; }

        /// <summary>
        /// 검사 중인 모든 객체에서 이 변수의 값 목록을 가져옵니다.
        /// </summary>
        /// <returns>각 객체의 변수 값 컬렉션입니다.</returns>
        IEnumerable<object?> GetValues();
    }
}
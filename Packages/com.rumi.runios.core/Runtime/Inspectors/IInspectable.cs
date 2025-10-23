#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Inspectors
{
    public interface IInspectable
    {
        string inspectionDisplayName { get; }

        bool instancesIsEmpty { get; }
        
        IReadOnlyList<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.All);

        /// <summary>
        /// 검사 중인 인스턴스의 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">가져온 인스턴스의 타입입니다.</param>
        /// <returns>타입을 성공적으로 가져올 시 true를 반환합니다.</returns>
        bool TryGetInspectionType([NotNullWhen(true)] out Type? type);
    }
}
#nullable enable
using System;

namespace RuniOS.Inspectors
{
    public interface IInspectorDictionaryElement : IInspectorVariableElement
    {
        object targetKey { get; set; }
        
        /// <summary>
        /// <b><see cref="IInspectorVariableElement.value"/></b>의 타입을 가져옵니다.<br/>
        /// <b><see cref="IInspectorVariableElement.value"/></b>의 값을 읽을 수 없거나 <see langword="null"/> 값인 경우에는 <see cref="object"/> 타입을 반환합니다.
        /// </summary>
        Type currentElementType { get; }
    }
}
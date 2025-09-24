#nullable enable
using System;
using System.Runtime.Serialization;

namespace RuniOS.Inspectors
{
    /// <summary>
    /// 인스펙터 시스템에서 발생하는 오류를 나타내는 예외입니다.
    /// </summary>
    [Serializable]
    public class InspectorException : Exception
    {
        /// <summary>
        /// 기본 생성자입니다.
        /// </summary>
        public InspectorException() { }

        /// <summary>
        /// 지정된 오류 메시지를 사용하여 <see cref="InspectorException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="message">예외를 설명하는 오류 메시지입니다.</param>
        public InspectorException(string message) : base(message) { }

        /// <summary>
        /// 지정된 오류 메시지와 내부 예외에 대한 참조를 사용하여 <see cref="InspectorException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="message">예외를 설명하는 오류 메시지입니다.</param>
        /// <param name="innerException">현재 예외의 원인인 예외입니다. <br/>내부 예외가 지정되지 않은 경우 <see langword="null"/>을 지정할 수 있습니다.</param>
        public InspectorException(string message, Exception innerException) : base(message, innerException) { }
        
        /// <summary>
        /// 직렬화된 데이터를 사용하여 <see cref="InspectorException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="info">직렬화된 개체 데이터를 보유하는 <see cref="SerializationInfo"/> 개체입니다.</param>
        /// <param name="context">소스 또는 대상에 대한 컨텍스트 정보를 포함하는 <see cref="StreamingContext"/> 개체입니다.</param>
        protected InspectorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
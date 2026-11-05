#nullable enable
namespace RuniOS.Inspectors
{
    /// <summary>
    /// 인스펙터의 요소에서 발생하는 오류를 나타내는 예외입니다.
    /// </summary>
    [Serializable]
    public class InspectorElementException : InspectorException
    {
        /// <summary>
        /// 대상 멤버의 이름입니다.
        /// <br/>이 예외를 발생시킨 필드 또는 프로퍼티의 이름을 저장합니다.
        /// </summary>
        public string? memberName { get; }

        /// <summary>
        /// 기본 생성자입니다.
        /// </summary>
        public InspectorElementException() { }

        /// <summary>
        /// 지정된 오류 메시지를 사용하여 <see cref="InspectorException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="message">예외를 설명하는 오류 메시지입니다.</param>
        public InspectorElementException(string message) : base(message) { }

        /// <summary>
        /// 지정된 오류 메시지와 내부 예외에 대한 참조를 사용하여 <see cref="InspectorException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="message">예외를 설명하는 오류 메시지입니다.</param>
        /// <param name="innerException">현재 예외의 원인인 예외입니다. <br/>내부 예외가 지정되지 않은 경우 <see langword="null"/>을 지정할 수 있습니다.</param>
        public InspectorElementException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// 멤버의 이름을 포함하는 오류 메시지를 사용하여 <see cref="InspectorException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="message">예외를 설명하는 오류 메시지입니다.</param>
        /// <param name="memberName">오류를 발생시킨 멤버의 이름입니다.</param>
        public InspectorElementException(string message, string memberName) : base(message) => this.memberName = memberName;
        
        /// <summary>
        /// 멤버의 이름을 포함하는 오류 메시지와 내부 예외에 대한 참조를 사용하여 <see cref="InspectorException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="message">예외를 설명하는 오류 메시지입니다.</param>
        /// <param name="memberName">오류를 발생시킨 멤버의 이름입니다.</param>
        /// <param name="innerException">현재 예외의 원인인 예외입니다. <br/>내부 예외가 지정되지 않은 경우 <see langword="null"/>을 지정할 수 있습니다.</param>
        public InspectorElementException(string message, string memberName, Exception innerException) : base(message, innerException) => this.memberName = memberName;
    }
}
#nullable enable
namespace RuniOS.Undos
{
    /// <summary>
    /// 언도 그룹을 식별하기 위한 고유 토큰 클래스입니다.<br/>
    /// 객체의 참조 비교(Reference Equality)를 통해 그룹을 구분합니다.
    /// </summary>
    public sealed class UndoGroupToken
    {
        /// <summary>
        /// 디버깅을 위해 지정된 토큰의 이름입니다.
        /// </summary>
        public string? debugName { get; } = null;

        public UndoGroupToken() { }
        public UndoGroupToken(string? debugName) => this.debugName = debugName;

        public override string ToString() => $"GroupToken({debugName ?? "Unnamed"})";
    }
}
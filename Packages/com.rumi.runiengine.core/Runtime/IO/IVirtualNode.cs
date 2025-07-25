#nullable enable
using System.ComponentModel;

namespace RuniEngine.IO
{
    /// <summary>
    /// 가상 파일 시스템 내의 노드를 나타내는 인터페이스입니다.<br/>
    /// 이 인터페이스는 가상 디렉토리 또는 가상 파일을 포함하는 노드의 기본 동작을 정의합니다.
    /// </summary>
    public interface IVirtualNode
    {
        /// <summary>
        /// 이 디렉토리가 속한 가상 파일 시스템의 최상위 루트 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용입니다.
        /// </summary>
        public VirtualDirectory? root { get; }

        /// <summary>
        /// 이 노드의 부모 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용이며, 부모 디렉토리가 없을 경우 <see langword="null"/>입니다.
        /// </summary>
        public VirtualDirectory? parent { get; }

        /// <summary>
        /// 이 노드의 이름입니다.<br/>
        /// 이 속성은 읽기 전용이며, 부모 디렉토리가 없을 경우 <see langword="null"/>입니다.
        /// </summary>
        public string? name { get; }

        /// <summary>
        /// 이 노드의 전체 경로입니다.<br/>
        /// 이 속성은 읽기 전용이며, 부모 디렉토리가 없을 경우 <see langword="null"/>입니다.
        /// </summary>
        public FilePath? fullPath { get; }

        /// <summary>
        /// 이 가상 파일 시스템 엔트리(디렉토리 또는 파일)가 독립적인 최상위 항목인지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 즉, 이 항목이 다른 가상 파일 시스템 엔트리의 하위가 아닌, 스스로 루트 역할을 하는지 여부를 나타냅니다.
        /// </summary>
        public bool isIndependent { get; }

        /// <summary>
        /// 이 디렉토리가 제거된 상태인지
        /// </summary>
        public bool isDeleted { get; }

        /// <summary>
        /// 이 <see cref="VirtualDirectory"/> 인스턴스를 상위 디렉토리에서 제거되어 유효하지 않은 상태로 설정합니다
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal void SetDeleted();
    }
}

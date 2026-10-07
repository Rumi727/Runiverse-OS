#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// Represents a snapshot of a file or directory entry discovered by an <see cref="IIOProvider"/>.<br/>
    /// Use <see cref="IONode.Bind"/> or <see cref="IOWriteNode.Bind"/> to create a node that points to this entry.
    /// <br/><br/>
    /// <see cref="IIOProvider"/>가 발견한 파일 또는 디렉터리 엔트리의 스냅샷을 나타냅니다.<br/>
    /// 이 엔트리를 가리키는 노드를 만들려면 <see cref="IONode.Bind"/> 또는 <see cref="IOWriteNode.Bind"/>를 사용합니다.
    /// </summary>
    /// <param name="path">
    /// The provider-relative path referenced by this entry.<br/>
    /// 이 엔트리가 참조하는 프로바이더 기준 경로입니다.
    /// </param>
    /// <param name="metaData">
    /// The metadata captured for this entry.<br/>
    /// 이 엔트리에 대해 캡처한 메타데이터입니다.
    /// </param>
    /// <param name="isDirectory">
    /// <see langword="true"/> if this entry is a directory; otherwise, <see langword="false"/>.<br/>
    /// 이 엔트리가 디렉터리이면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
    /// </param>
    public readonly record struct IOEntry(RuniPath path, FileMetaData metaData, bool isDirectory);
}

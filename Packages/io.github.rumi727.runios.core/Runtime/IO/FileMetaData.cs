#nullable enable
using System.IO;

namespace RuniOS.IO
{
    /// <summary>
    /// Represents metadata captured for a file or directory entry.<br/>
    /// Members may be <see langword="null"/> when the backing provider cannot supply that information.
    /// <br/><br/>
    /// 파일 또는 디렉터리 엔트리에서 캡처한 메타데이터를 나타냅니다.<br/>
    /// 기반 프로바이더가 해당 정보를 제공할 수 없는 경우 멤버 값은 <see langword="null"/>일 수 있습니다.
    /// </summary>
    /// <param name="name">
    /// The file or directory name.<br/>
    /// 파일 또는 디렉터리 이름입니다.
    /// </param>
    /// <param name="size">
    /// The file size in bytes, or <see langword="null"/> for directories or unknown sizes.<br/>
    /// 파일 크기(바이트)이며, 디렉터리이거나 크기를 알 수 없는 경우 <see langword="null"/>입니다.
    /// </param>
    /// <param name="creationTime">
    /// The creation time in UTC, when available.<br/>
    /// 제공 가능한 경우 UTC 기준 생성 시간입니다.
    /// </param>
    /// <param name="lastAccessTime">
    /// The last access time in UTC, when available.<br/>
    /// 제공 가능한 경우 UTC 기준 마지막 접근 시간입니다.
    /// </param>
    /// <param name="lastWriteTime">
    /// The last write time in UTC, when available.<br/>
    /// 제공 가능한 경우 UTC 기준 마지막 수정 시간입니다.
    /// </param>
    /// <param name="attributes">
    /// The file-system attributes, when available.<br/>
    /// 제공 가능한 경우 파일 시스템 특성입니다.
    /// </param>
    public readonly partial record struct FileMetaData(string? name, long? size, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, FileAttributes? attributes)
    {
        /// <summary>
        /// Initializes metadata with only a file or directory name.<br/>
        /// 파일 또는 디렉터리 이름만 가진 메타데이터를 초기화합니다.
        /// </summary>
        /// <param name="name">
        /// The file or directory name.<br/>
        /// 파일 또는 디렉터리 이름입니다.
        /// </param>
        public FileMetaData(string name) : this(name, null, null, null, null, null) { }

        /// <summary>
        /// Initializes metadata with a file name and size.<br/>
        /// 파일 이름과 크기를 가진 메타데이터를 초기화합니다.
        /// </summary>
        /// <param name="name">
        /// The file name.<br/>
        /// 파일 이름입니다.
        /// </param>
        /// <param name="size">
        /// The file size in bytes.<br/>
        /// 파일 크기(바이트)입니다.
        /// </param>
        public FileMetaData(string name, long size) : this(name, size, null, null, null, null) { }

        /// <summary>
        /// 현재 파일 메타데이터와 <paramref name="other"/>가 동일한 파일 리비전을 나타내는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="lastWriteTime"/>은 리비전 비교의 필수 기준입니다.
        /// 두 값 중 하나라도 없거나 서로 다르면 동일한 리비전으로 취급하지 않습니다.
        /// </para>
        /// <para>
        /// <see cref="size"/>와 <see cref="creationTime"/>은 양쪽 모두 값이 제공되는 경우에만 추가로 비교하며,
        /// 값이 서로 다르면 동일한 리비전으로 취급하지 않습니다.
        /// </para>
        /// <para>
        /// <see cref="lastAccessTime"/>과 <see cref="attributes"/>는 파일 내용의 리비전을 안정적으로 나타내지 않으므로 비교하지 않습니다.
        /// </para>
        /// </remarks>
        /// <param name="other">비교할 파일 메타데이터입니다.</param>
        /// <returns>
        /// 동일한 파일 리비전으로 판단할 수 있으면 <see langword="true"/>,
        /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool IsSameRevision(FileMetaData other)
        {
            // 마지막 수정 시간은 양쪽 모두 존재해야 하며 서로 같아야 합니다.
            if (lastWriteTime is not { } thisLastWriteTime || other.lastWriteTime is not { } otherLastWriteTime || thisLastWriteTime != otherLastWriteTime)
                return false;

            // 파일 크기는 양쪽 모두 제공되는 경우에만 비교하며, 다르면 다른 리비전으로 취급합니다.
            if (size is { } thisSize && other.size is { } otherSize && thisSize != otherSize)
                return false;

            // 생성 시간은 양쪽 모두 제공되는 경우에만 비교하며, 다르면 다른 리비전으로 취급합니다.
            if (creationTime is { } thisCreationTime && other.creationTime is { } otherCreationTime && thisCreationTime != otherCreationTime)
                return false;

            // 마지막 접근 시간과 파일 시스템 특성은 리비전 판정에 사용하지 않습니다.
            return true;
        }
    }
}

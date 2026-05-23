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
    }
}

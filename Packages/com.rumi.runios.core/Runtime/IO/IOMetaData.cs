#nullable enable
using System.IO;

namespace RuniOS.IO
{
    /// <summary>
    /// 파일 또는 디렉토리의 세부 상태 정보(메타데이터)를 나타냅니다.
    /// 지원되지 않는 정보의 경우 <see langword="null"/>일 수 있습니다.
    /// </summary>
    /// <param name="name">이 파일 또는 디렉토리의 이름입니다.</param>
    /// <param name="size">파일의 크기(바이트)입니다.</param>
    /// <param name="creationTime">생성된 시간(UTC)입니다.</param>
    /// <param name="lastAccessTime">마지막으로 접근한 시간(UTC)입니다.</param>
    /// <param name="lastWriteTime">마지막으로 수정된 시간(UTC)입니다.</param>
    /// <param name="attributes">파일 또는 디렉토리의 특성(숨김, 읽기 전용 등)입니다.</param>
    public readonly partial record struct IOMetaData(string? name, long? size, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, FileAttributes? attributes)
    {
        /// <param name="name">이 파일 또는 디렉토리의 이름입니다.</param>
        public IOMetaData(string name) : this(name, null, null, null, null, null) { }

        /// <param name="name">이 파일 또는 디렉토리의 이름입니다.</param>
        /// <param name="size">파일의 크기(바이트)입니다.</param>
        public IOMetaData(string name, long size) : this(name, size, null, null, null, null) { }
    }
}
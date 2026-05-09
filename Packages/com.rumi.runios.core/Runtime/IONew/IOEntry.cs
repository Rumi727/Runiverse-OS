#nullable enable
using RuniOS.IO;

namespace RuniOS.IONew
{
    /// <summary>
    /// 파일 시스템에서 검색된 특정 파일 또는 디렉토리의 정보(스냅샷)를 나타냅니다.
    /// 이 객체는 순수한 데이터 구조체이며, 실제 파일을 조작하려면 <see cref="IONode.Bind"/>를 사용하여 노드로 변환해야 합니다.
    /// </summary>
    /// <param name="path">이 엔트리가 참조하는 가상 파일 시스템 상의 절대 경로입니다.</param>
    /// <param name="metaData">파일 또는 디렉토리의 메타데이터(이름, 크기, 시간 등)입니다.</param>
    /// <param name="isDirectory">이 엔트리가 디렉토리인지 여부를 나타냅니다.</param>
    public readonly record struct IOEntry(FilePath path, IOMetaData metaData, bool isDirectory);
}
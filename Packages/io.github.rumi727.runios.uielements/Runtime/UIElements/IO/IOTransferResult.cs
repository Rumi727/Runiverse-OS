#nullable enable
using RuniOS.IO;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// <see cref="IOTransfer"/>가 처리를 마친 항목 하나입니다.
    /// </summary>
    /// <param name="source">옮기거나 복사하거나 링크한 원본 항목입니다. 옮긴 뒤에는 이 경로에 항목이 없습니다.</param>
    /// <param name="destination">항목이 들어간 대상입니다. 이름이 겹쳐 이름을 바꿨다면 바꾼 경로입니다.</param>
    /// <param name="overwritten">이미 있던 <paramref name="destination"/>의 파일을 덮어썼는지 나타냅니다.</param>
    public readonly record struct IOTransferResult(IONode source, IONode destination, bool overwritten);
}

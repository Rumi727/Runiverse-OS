#nullable enable
using RuniOS.IO;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// <see cref="IODragManipulator"/>로 끄는 IO 항목입니다.
    /// </summary>
    /// <param name="source">항목을 끌기 시작한 트리 뷰입니다. 이동한 뒤 원본 목록을 새로 고치는 데 씁니다.</param>
    /// <param name="provider">끌기 시작할 때 <paramref name="source"/>가 가리키던 프로바이더입니다. 끄는 동안 원본 트리 뷰가 다른 위치로 이동할 수 있으므로 따로 기록합니다.</param>
    /// <param name="entries">끄는 항목입니다. 함께 끄는 폴더 안의 항목은 들어 있지 않습니다.</param>
    public sealed record IODragData(IONodeMultiColumnTreeView source, IIOProvider provider, IOEntry[] entries);
}

#nullable enable
using RuniOS.IO;
using UnityEngine;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// IO 항목을 끌어다 놓을 수 있는 요소입니다. <see cref="IODragManipulator"/>가 포인터 아래 요소부터 조상으로 올라가며 찾습니다.
    /// </summary>
    /// <remarks>
    /// 끄는 동안 어떻게 보일지는 이 요소가 <see cref="OnDragOver"/>와 <see cref="OnDragLeave"/>를 받아 직접 정합니다.
    /// </remarks>
    public interface IIODropTarget
    {
        /// <summary>
        /// <paramref name="position"/>에 놓을 때 항목이 들어갈 폴더를 가져옵니다.
        /// </summary>
        /// <param name="position">패널 좌표의 포인터 위치입니다.</param>
        /// <returns>이 요소에 놓을 수 없으면 <see langword="false"/>입니다.</returns>
        bool TryGetDropDirectory(Vector2 position, out IONode directory);

        /// <summary>
        /// 끄는 항목이 이 요소 위에 있는 동안, 포인터가 움직이거나 보조 키가 바뀔 때마다 호출됩니다.
        /// </summary>
        /// <param name="data">끄는 항목입니다.</param>
        /// <param name="position">패널 좌표의 포인터 위치입니다.</param>
        /// <param name="canDrop">지금 놓으면 항목을 옮기거나 복사할 수 있는지 여부입니다.</param>
        void OnDragOver(IODragData data, Vector2 position, bool canDrop);

        /// <summary>
        /// 끄는 항목이 이 요소를 벗어나거나, 이 요소 위에서 드래그가 끝나면 호출됩니다.
        /// </summary>
        void OnDragLeave();
    }
}

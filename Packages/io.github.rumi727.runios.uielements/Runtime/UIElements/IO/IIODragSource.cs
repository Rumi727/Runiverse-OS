#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// <see cref="IODragManipulator"/>로 IO 항목을 끌기 시작할 수 있는 요소입니다.
    /// </summary>
    /// <remarks>
    /// 끄는 동안 포인터를 따라다니는 고스트는 이 요소가 <see cref="OnDragStart"/>에서 만들고, <see cref="OnDragUpdate"/>에서 직접 꾸밉니다.
    /// </remarks>
    public interface IIODragSource
    {
        /// <summary>
        /// 누를 때 끌 항목을 가져옵니다.
        /// </summary>
        /// <returns>끌 항목이 없으면 <see langword="null"/>입니다.</returns>
        IODragData? GetDragData(PointerDownEvent evt);

        /// <summary>
        /// 누른 채 충분히 움직여 드래그를 시작할 때 호출되며, 포인터를 따라다닐 고스트를 만듭니다.
        /// </summary>
        /// <remarks>
        /// 고스트는 패널 맨 위에 붙고, 조작기가 위치를 정합니다. 포인터 아래의 놓을 대상을 가리지 않도록 조작기가 고스트와 그 자식의 <see cref="VisualElement.pickingMode"/>를 <see cref="PickingMode.Ignore"/>로 바꿉니다.
        /// 고스트는 이 요소의 조상에서 받던 스타일을 받지 못하므로, 필요한 스타일은 고스트에 직접 붙여야 합니다.
        /// </remarks>
        VisualElement OnDragStart(IODragData data);

        /// <summary>
        /// 끄는 동안 포인터가 움직이거나 보조 키가 바뀔 때마다 호출됩니다.
        /// </summary>
        /// <param name="canDrop">지금 놓으면 항목을 옮기거나 복사할 수 있는지 여부입니다.</param>
        /// <param name="operation">놓으면 할 동작입니다. 놓을 수 없거나, 보조 키가 없어 놓을 때 메뉴로 물으면 <see langword="null"/>입니다.</param>
        void OnDragUpdate(IODragData data, bool canDrop, IODropOperation? operation);

        /// <summary>
        /// 놓거나 취소해 드래그가 끝나면 호출됩니다. 고스트는 조작기가 이미 떼어 냈습니다.
        /// </summary>
        void OnDragEnd(IODragData data);
    }
}

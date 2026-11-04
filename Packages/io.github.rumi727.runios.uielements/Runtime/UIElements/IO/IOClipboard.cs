#nullable enable
using RuniOS.IO;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// IO 항목을 복사하거나 잘라내 둔 앱 내부 클립보드입니다. 항목을 들고 있기만 하며, 붙여넣기와 새로 고침은 호출자가 합니다.
    /// 시스템 클립보드는 쓰지 않고, 도메인을 다시 불러오면 비워집니다.
    /// </summary>
    public static class IOClipboard
    {
        /// <summary>복사하거나 잘라낸 항목입니다. 비어 있으면 빈 배열입니다.</summary>
        public static IONode[] nodes { get; private set; } = [];

        /// <summary>항목이 복사가 아니라 잘라낸 것인지 나타냅니다.</summary>
        public static bool isCut { get; private set; }

        /// <summary><see cref="Set"/>이나 <see cref="Clear"/>로 내용이 바뀌면 발생합니다.</summary>
        public static event Action? changed;

        public static void Set(IONode[] nodes, bool isCut)
        {
            IOClipboard.nodes = nodes;
            IOClipboard.isCut = isCut;
            changed?.Invoke();
        }

        public static void Clear() => Set([], false);
    }
}

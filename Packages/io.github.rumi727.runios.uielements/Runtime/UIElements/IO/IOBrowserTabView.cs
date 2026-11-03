#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// <see cref="IOBrowserTab"/>을 담는 <see cref="TabView"/>입니다.
    /// 탭 헤더는 탭이 아니라 이 요소의 자식이므로, 헤더 위에 놓는 것을 이 요소가 받아 그 헤더의 탭에 전달합니다.
    /// </summary>
    public class IOBrowserTabView : TabView, IIODropTarget
    {
        IOBrowserTab? hoveredTab;

        /// <summary>
        /// 위치가 <paramref name="directory"/>인 탭의 내용을 다시 읽어 옵니다.
        /// </summary>
        public void Refresh(IONode directory)
        {
            foreach (IOBrowserTab tab in Children().OfType<IOBrowserTab>())
            {
                if (tab.node == directory)
                    tab.Refresh().Forget();
            }
        }

        bool IIODropTarget.TryGetDropDirectory(Vector2 position, out IONode directory)
        {
            directory = default;
            return GetTab(position) is IIODropTarget tab && tab.TryGetDropDirectory(position, out directory);
        }

        void IIODropTarget.OnDragOver(IODragData data, Vector2 position, bool canDrop)
        {
            IOBrowserTab? tab = GetTab(position);
            if (hoveredTab != tab)
                ((IIODropTarget?)hoveredTab)?.OnDragLeave();

            hoveredTab = tab;
            ((IIODropTarget?)tab)?.OnDragOver(data, position, canDrop);
        }

        void IIODropTarget.OnDragLeave()
        {
            ((IIODropTarget?)hoveredTab)?.OnDragLeave();
            hoveredTab = null;
        }

        IOBrowserTab? GetTab(Vector2 position) => Children().OfType<IOBrowserTab>().FirstOrDefault(x => x.tabHeader.worldBound.Contains(position));
    }
}

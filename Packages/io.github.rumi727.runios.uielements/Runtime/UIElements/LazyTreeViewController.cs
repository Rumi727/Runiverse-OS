#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// A <see cref="TreeView"/> controller whose children are loaded by a <see cref="LazyTreeLoader{T}"/>.<br/>
    /// 자식을 <see cref="LazyTreeLoader{T}"/>로 로드하는 <see cref="TreeView"/> 컨트롤러입니다.
    /// </summary>
    public class LazyTreeViewController<T>(LazyTreeLoader<T> loader) : DefaultTreeViewController<T>
    {
        public LazyTreeLoader<T> loader { get; } = loader;

        public override bool HasChildren(int id) => Exists(id) && loader.HasChildren(id, GetDataForId(id), base.HasChildren(id));

        protected override void BindItem(VisualElement element, int index)
        {
            base.BindItem(element, index);
            loader.OnBindItem(GetIdForIndex(index));
        }
    }
}

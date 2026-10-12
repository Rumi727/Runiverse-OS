#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// A <see cref="MultiColumnTreeView"/> controller whose children are loaded by a <see cref="LazyTreeLoader{T}"/>.<br/>
    /// 자식을 <see cref="LazyTreeLoader{T}"/>로 로드하는 <see cref="MultiColumnTreeView"/> 컨트롤러입니다.
    /// </summary>
    // MultiColumnTreeView는 정렬 상태 리스트를 컨트롤러와 공유하므로 sortedColumns가 반환하는 같은 인스턴스를 넘겨야 합니다.
    public class LazyMultiColumnTreeViewController<T>(MultiColumnTreeView treeView, LazyTreeLoader<T> loader) : DefaultMultiColumnTreeViewController<T>(treeView.columns, treeView.sortColumnDescriptions, (List<SortColumnDescription>)treeView.sortedColumns)
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

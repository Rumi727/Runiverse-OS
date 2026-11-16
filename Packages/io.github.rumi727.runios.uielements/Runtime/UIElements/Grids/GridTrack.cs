#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// One row of a <see cref="GridView"/>: a <see cref="header"/> on the left and a <see cref="layer"/> on the right.<br/>
    /// The row is as high as its layer, so the header always matches it.
    /// <br/><br/>
    /// <see cref="GridView"/>의 한 줄입니다. 왼쪽에 <see cref="header"/>, 오른쪽에 <see cref="layer"/>가 있습니다.<br/>
    /// 줄의 높이는 레이어를 따르므로 헤더 높이가 항상 레이어와 같습니다.
    /// </summary>
    public class GridTrack : VisualElement
    {
        /// <summary>
        /// The USS class name of tracks.<br/>
        /// 트랙의 USS 클래스 이름입니다.
        /// </summary>
        public const string ussClassName = "runios-grid-track";

        /// <summary>
        /// The USS class name of the header of tracks.<br/>
        /// 트랙 헤더의 USS 클래스 이름입니다.
        /// </summary>
        public const string headerUssClassName = ussClassName + "__header";

        /// <summary>
        /// The USS class name added to the layer of tracks.<br/>
        /// 트랙의 레이어에 추가되는 USS 클래스 이름입니다.
        /// </summary>
        public const string layerUssClassName = ussClassName + "__layer";

        /// <summary>
        /// Initializes a track that shows <paramref name="layer"/>.<br/>
        /// <paramref name="layer"/>를 보여 주는 트랙을 초기화합니다.
        /// </summary>
        /// <param name="layer">
        /// The layer of the track.<br/>
        /// 트랙의 레이어입니다.
        /// </param>
        public GridTrack(GridLayer layer)
        {
            AddToClassList(ussClassName);

            header = new VisualElement { name = "header" };
            header.AddToClassList(headerUssClassName);
            hierarchy.Add(header);

            this.layer = layer;
            layer.AddToClassList(layerUssClassName);
            hierarchy.Add(layer);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (GetFirstAncestorOfType<GridView>() is { } view)
                {
                    header.style.width = view.headerWidth;
                    layer.style.marginLeft = view.headerWidth;
                }
            });
        }

        /// <summary>
        /// Gets the header on the left. Add elements to it to show information or columns for the track.<br/>
        /// Its width is <see cref="GridView.headerWidth"/>. It is laid over the track without taking part in its height, so content taller than the layer is clipped.
        /// <br/><br/>
        /// 왼쪽의 헤더를 가져옵니다. 트랙의 정보나 열을 보여 주려면 여기에 요소를 추가하세요.<br/>
        /// 폭은 <see cref="GridView.headerWidth"/>입니다. 트랙 높이에 관여하지 않고 트랙 위에 놓이므로, 레이어보다 높은 내용은 잘립니다.
        /// </summary>
        public VisualElement header { get; }

        /// <summary>
        /// Gets the layer of the track.<br/>
        /// 트랙의 레이어를 가져옵니다.
        /// </summary>
        public GridLayer layer { get; }
    }
}

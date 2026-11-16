#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    public partial class GridView
    {
        /// <summary>
        /// Converts a panel position to a pixel of <see cref="axis"/>.<br/>
        /// 패널 위치를 <see cref="axis"/>의 픽셀로 변환합니다.
        /// </summary>
        /// <param name="panelPosition">
        /// The position in panel coordinates.<br/>
        /// 패널 좌표의 위치입니다.
        /// </param>
        /// <returns>
        /// The pixel measured from the left edge of the item area.<br/>
        /// 아이템 영역 왼쪽 끝부터 잰 픽셀을 반환합니다.
        /// </returns>
        public float GetAxisPixel(Vector2 panelPosition) => panelPosition.x - viewport.worldBound.x - headerWidth;

        /// <summary>
        /// Finds what is at <paramref name="panelPosition"/>: the axis position, ruler, track, layer, and item.<br/>
        /// Use it to build pointer handling of your own; <see cref="GridSelectManipulator"/> uses it too.
        /// <br/><br/>
        /// <paramref name="panelPosition"/>에 있는 것들(축 위치, 눈금자, 트랙, 레이어, 아이템)을 찾습니다.<br/>
        /// 직접 포인터 처리를 만들 때 쓰세요. <see cref="GridSelectManipulator"/>도 이것을 씁니다.
        /// </summary>
        /// <param name="panelPosition">
        /// The position in panel coordinates.<br/>
        /// 패널 좌표의 위치입니다.
        /// </param>
        /// <param name="button">
        /// The mouse button to report, or <c>-1</c>.<br/>
        /// 보고할 마우스 버튼이며, 없으면 <c>-1</c>입니다.
        /// </param>
        /// <param name="clickCount">
        /// The click count to report.<br/>
        /// 보고할 클릭 수입니다.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys to report.<br/>
        /// 보고할 보조 키입니다.
        /// </param>
        /// <param name="actionKey">
        /// Whether the platform action key is held down.<br/>
        /// 플랫폼 동작 키가 눌려 있는지 여부입니다.
        /// </param>
        /// <returns>
        /// What is at the position. Only items on screen are found.<br/>
        /// 그 위치에 있는 것들을 반환합니다. 화면에 보이는 아이템만 찾습니다.
        /// </returns>
        public GridPointerArgs CreatePointerArgs(Vector2 panelPosition, int button = -1, int clickCount = 0, EventModifiers modifiers = EventModifiers.None, bool actionKey = false)
        {
            double position = axis.ToPosition(GetAxisPixel(panelPosition));

            GridRuler? ruler = null;
            foreach (GridRuler candidate in rulers)
            {
                if (candidate.worldBound.Contains(panelPosition))
                {
                    ruler = candidate;
                    break;
                }
            }

            GridTrack? track = null;
            GridLayer? layer = null;
            GridItem? item = null;
            double verticalPosition = 0;
            if (ruler == null && scrollView.contentViewport.worldBound.Contains(panelPosition))
            {
                foreach (GridTrack candidate in tracks)
                {
                    if (!candidate.worldBound.Contains(panelPosition))
                        continue;

                    track = candidate;
                    if (candidate.layer.worldBound.Contains(panelPosition))
                    {
                        layer = candidate.layer;
                        Vector2 local = layer.WorldToLocal(panelPosition);
                        verticalPosition = layer.GetVerticalPosition(local.y);
                        item = layer.HitTest(local);
                    }

                    break;
                }
            }

            return new GridPointerArgs
            {
                button = button,
                clickCount = clickCount,
                modifiers = modifiers,
                actionKey = actionKey,
                panelPosition = panelPosition,
                position = position,
                snappedPosition = Snap(position, modifiers),
                verticalPosition = verticalPosition,
                item = item,
                layer = layer,
                track = track,
                ruler = ruler
            };
        }
    }
}

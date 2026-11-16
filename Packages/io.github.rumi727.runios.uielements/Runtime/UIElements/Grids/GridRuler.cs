#nullable enable
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A ruler at the top of a <see cref="GridView"/> that shows the lines and labels of one <see cref="GridScale"/>.<br/>
    /// Several rulers can show a main unit and sub units at the same time.
    /// <br/><br/>
    /// <see cref="GridView"/> 위쪽에서 <see cref="GridScale"/> 하나의 선과 라벨을 보여 주는 눈금자입니다.<br/>
    /// 눈금자를 여러 개 두면 메인 단위와 서브 단위를 함께 보여 줄 수 있습니다.
    /// </summary>
    /// <remarks>
    /// Level-0 lines go from top to bottom, level-1 lines cover half of the height, and level-2 lines cover a quarter. Labels use the font size and color of the ruler.<br/>
    /// 0단계 선은 위에서 아래까지, 1단계 선은 높이의 절반, 2단계 선은 4분의 1을 차지합니다. 라벨은 눈금자의 글자 크기와 색을 씁니다.
    /// </remarks>
    public class GridRuler : VisualElement
    {
        /// <summary>
        /// The USS class name of rulers.<br/>
        /// 눈금자의 USS 클래스 이름입니다.
        /// </summary>
        public const string ussClassName = "runios-grid-ruler";

        static readonly CustomStyleProperty<Color> lineColorProperty = new("--runios-grid-ruler-line-color");

        readonly List<GridLine> lines = [];
        readonly List<(Rect rect, Color color)> rects = [];
        Color lineColor = new(0.5f, 0.5f, 0.5f, 0.8f);
        GridView? view;

        /// <summary>
        /// Initializes a ruler that shows <paramref name="scale"/>.<br/>
        /// <paramref name="scale"/>을 보여 주는 눈금자를 초기화합니다.
        /// </summary>
        /// <param name="scale">
        /// The scale to show.<br/>
        /// 보여 줄 스케일입니다.
        /// </param>
        public GridRuler(GridScale scale)
        {
            this.scale = scale;
            AddToClassList(ussClassName);

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(_ => view = GetFirstAncestorOfType<GridView>());
            RegisterCallback<DetachFromPanelEvent>(_ => view = null);
            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(lineColorProperty, out Color color))
                    lineColor = color;

                MarkDirtyRepaint();
            });
        }

        /// <summary>
        /// Gets or sets the scale shown by the ruler.<br/>
        /// 눈금자가 보여 주는 스케일을 가져오거나 설정합니다.
        /// </summary>
        public GridScale scale
        {
            get;
            set
            {
                field = value;
                MarkDirtyRepaint();
            }
        }

        void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (view is not { } gridView)
                return;

            GridAxis axis = gridView.axis;
            float height = contentRect.height;

            lines.Clear();
            scale.GetLines(axis.GetRange(), axis.pixelsPerUnit, true, lines);

            rects.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                GridLine line = lines[i];
                float lineHeight = line.level switch
                {
                    <= 0 => height,
                    1 => height * 0.5f,
                    _ => height * 0.25f
                };

                float x = MathF.Round(axis.ToPixel(line.position));
                rects.Add((new Rect(x, height - lineHeight, 1, lineHeight), lineColor));
            }

            context.DrawRects(CollectionsMarshal.AsSpan(rects));

            float fontSize = resolvedStyle.fontSize;
            Color textColor = resolvedStyle.color;
            for (int i = 0; i < lines.Count; i++)
            {
                GridLine line = lines[i];
                if (!string.IsNullOrEmpty(line.label))
                    context.DrawText(line.label, new Vector2(MathF.Round(axis.ToPixel(line.position)) + 3, 0), fontSize, textColor);
            }
        }
    }
}

#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.Resource;
using RuniOS.IO;
using RuniOS.UIElements;
using System.Collections.Immutable;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Resource
{
    /// <summary>
    /// Shows a <see cref="PackDrawer"/> with its title, body, Apply/Revert footer, and preview.<br/>
    /// The body and the preview use the UI Toolkit element that the drawer creates, or its IMGUI methods when it creates none.
    /// <br/><br/>
    /// <see cref="PackDrawer"/>를 제목, 본문, 적용/되돌리기 푸터, 미리보기와 함께 보여줍니다.<br/>
    /// 본문과 미리보기는 드로어가 만든 UI Toolkit 요소를 쓰고, 만들지 않으면 드로어의 IMGUI 메서드를 씁니다.
    /// </summary>
    /// <remarks>
    /// This element only shows the drawer. The owner of the drawer calls <see cref="PackDrawer.OnEnable"/> and <see cref="PackDrawer.OnDisable"/>, and decides what happens to unsaved changes.<br/>
    /// When any target is not in an <see cref="IWritableIOProvider"/>, the body is disabled and the footer is hidden.
    /// <br/><br/>
    /// 이 요소는 드로어를 보여주기만 합니다. <see cref="PackDrawer.OnEnable"/>과 <see cref="PackDrawer.OnDisable"/> 호출과, 적용하지 않은 변경 사항의 처리는 드로어를 가진 쪽이 정합니다.<br/>
    /// 대상 중 하나라도 <see cref="IWritableIOProvider"/>에 있지 않으면 본문을 비활성화하고 푸터를 숨깁니다.
    /// </remarks>
    public class PackInspector : VisualElement
    {
        public const string ussClassName = "runios-pack-inspector";
        public const string titleUssClassName = ussClassName + "__title";
        public const string splitViewUssClassName = ussClassName + "__split-view";
        public const string scrollViewUssClassName = ussClassName + "__scroll-view";
        public const string bodyUssClassName = ussClassName + "__body";
        public const string footerUssClassName = ussClassName + "__footer";
        public const string footerButtonUssClassName = ussClassName + "__footer-button";
        public const string previewUssClassName = ussClassName + "__preview";
        public const string previewHeaderUssClassName = ussClassName + "__preview-header";
        public const string previewContentUssClassName = ussClassName + "__preview-content";

        // Unity의 ObjectPreview.DrawPreview와 같은 값입니다.
        const int previewLabelHeight = 12;
        const int previewMinSize = 55;
        const int gridTargetCount = 25;
        const int gridSpacing = 10;
        const int previewLabelPadding = 5;

        // GUI 좌표는 y가 아래로 커지므로 RuniOS.RectOffset이 아니라 Unity의 RectOffset을 씁니다.
        static readonly UnityEngine.RectOffset previewGridPadding = new UnityEngine.RectOffset(16, 16, 20, 25);

        /// <summary>
        /// Gets or sets the drawer to show, or <see langword="null"/> to show nothing.<br/>
        /// 보여줄 드로어를 가져오거나 설정하며, 아무것도 보여주지 않으려면 <see langword="null"/>입니다.
        /// </summary>
        public PackDrawer? drawer
        {
            get;
            set
            {
                if (field == value)
                    return;

                if (field != null)
                {
                    field.repaintAction -= Repaint;
                    field.onDirtyStateChanged -= UpdateFooter;
                }

                field = value;

                if (field != null)
                {
                    field.repaintAction += Repaint;
                    field.onDirtyStateChanged += UpdateFooter;
                }

                Rebuild();
            }
        }

        /// <summary>
        /// Gets or sets whether the drawer is shown in debug mode. Changing it creates the body again.<br/>
        /// 드로어를 디버그 모드로 보여줄지 가져오거나 설정합니다. 바꾸면 본문을 다시 만듭니다.
        /// </summary>
        public bool isDebug
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Rebuild();
            }
        }

        /// <summary>
        /// Gets or sets whether the Apply button can be used while the drawer is dirty. The Revert button is not affected.<br/>
        /// 드로어가 더티일 때 적용 버튼을 쓸 수 있는지 가져오거나 설정합니다. 되돌리기 버튼에는 영향을 주지 않습니다.
        /// </summary>
        public bool canApply
        {
            get;
            set
            {
                field = value;
                UpdateFooter();
            }
        } = true;

        /// <summary>
        /// Occurs when one of several previewed targets is double-clicked, with that target.<br/>
        /// 미리 보는 여러 대상 중 하나를 더블클릭하면 그 대상과 함께 발생합니다.
        /// </summary>
        /// <remarks>
        /// This element does not change any selection by itself. The owner decides what to do, such as selecting only that target.<br/>
        /// 이 요소는 선택을 직접 바꾸지 않습니다. 그 대상만 선택하는 것처럼 무엇을 할지는 이 요소를 가진 쪽이 정합니다.
        /// </remarks>
        public event Action<IONode>? previewTargetActivated;

        public Label title { get; }

        public TwoPaneSplitView splitView { get; }

        public ScrollView scrollView { get; }

        public VisualElement footer { get; }
        public Button apply { get; }
        public Button revert { get; }

        public VisualElement preview { get; }
        public IMGUIContainer previewHeader { get; }

        VisualElement? body;
        VisualElement? previewContent;

        public PackInspector()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            title = new Label { name = "title" };
            title.AddToClassList(titleUssClassName);
            Add(title);

            splitView = new TwoPaneSplitView(1, 200, TwoPaneSplitViewOrientation.Vertical) { name = "split-view" };
            splitView.AddToClassList(splitViewUssClassName);
            {
                scrollView = new ScrollView { name = "scroll-view" };
                scrollView.AddToClassList(scrollViewUssClassName);
                {
                    footer = new VisualElement { name = "footer" };
                    footer.AddToClassList(footerUssClassName);
                    {
                        apply = new Button(() => drawer?.SaveChanges().Forget()) { name = "apply" };
                        apply.AddToClassList(footerButtonUssClassName);
                        footer.Add(apply);

                        revert = new Button(() => drawer?.DiscardChanges().Forget()) { name = "revert" };
                        revert.AddToClassList(footerButtonUssClassName);
                        footer.Add(revert);
                    }
                    scrollView.Add(footer);
                }
                splitView.Add(scrollView);

                preview = new VisualElement { name = "preview" };
                preview.AddToClassList(previewUssClassName);
                {
                    previewHeader = new IMGUIContainer(DrawPreviewHeader) { name = "preview-header" };
                    previewHeader.AddToClassList(previewHeaderUssClassName);
                    preview.Add(previewHeader);
                }
                splitView.Add(preview);
            }
            Add(splitView);

            this.AddManipulator(new LocalizationManipulator(OnLanguageChanged));

            Rebuild();
        }

        void OnLanguageChanged()
        {
            apply.text = GetTextOrKey("runios-editor:gui.apply");
            revert.text = GetTextOrKey("runios-editor:gui.revert");

            title.text = GetTitle();
        }

        // 드로어나 디버그 모드가 바뀌면 본문과 미리보기를 새로 만듭니다.
        void Rebuild()
        {
            body?.RemoveFromHierarchy();
            body = null;

            previewContent?.RemoveFromHierarchy();
            previewContent = null;

            title.text = GetTitle();

            if (drawer == null)
            {
                footer.style.display = DisplayStyle.None;
                splitView.CollapseChild(1);

                return;
            }

            bool isReadOnly = drawer.targets.Any(static x => x.provider is not IWritableIOProvider);

            // IMGUIContainer는 비활성화되면 GUI.enabled를 끈 채로 그리므로, 두 방식 모두 SetEnabled로 편집을 막습니다.
            // IMGUI 본문은 foldout 화살표가 컨트롤 왼쪽 바깥에 그려지므로, USS 여백 대신 DrawBody 안에서 직접 여백을 줍니다.
            if (drawer.CreateInspectorGUI(isDebug) is { } inspectorGUI)
            {
                body = inspectorGUI;
                body.AddToClassList(bodyUssClassName);
            }
            else
                body = new IMGUIContainer(DrawBody);
            body.SetEnabled(!isReadOnly);
            scrollView.Insert(0, body);

            footer.style.display = drawer.needsApplyRevert && !isReadOnly ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateFooter();

            // 대상이 여러 개면 Unity 인스펙터처럼 모든 대상을 IMGUI 격자로 그리며, 드로어의 UI Toolkit 미리보기는 대상이 하나일 때만 씁니다.
            if (drawer.HasPreviewGUI() && drawer.targets.Length > 0)
            {
                if (drawer.targets.Length == 1 && drawer.CreatePreview(drawer.targets[0]) is { } element)
                    previewContent = element;
                else
                {
                    IMGUIContainer container = new IMGUIContainer();
                    container.onGUIHandler = () => DrawPreview(container);

                    previewContent = container;
                }

                previewContent.AddToClassList(previewContentUssClassName);
                preview.Add(previewContent);

                splitView.UnCollapse();
            }
            else
                splitView.CollapseChild(1);
        }

        void UpdateFooter()
        {
            bool isDirty = drawer?.isDirty ?? false;

            apply.SetEnabled(isDirty && canApply);
            revert.SetEnabled(isDirty);
        }

        void Repaint()
        {
            body?.MarkDirtyRepaint();
            previewHeader.MarkDirtyRepaint();
            previewContent?.MarkDirtyRepaint();
        }

        string GetTitle()
        {
            if (drawer == null)
                return string.Empty;

            if (drawer.targetTitle != null)
                return drawer.targetTitle;

            string typeName = drawer.targetTypeName;
            string settingsText = GetTextOrKey("runios-editor:gui.settings");
            if (drawer.target is not { } target)
                return $"{drawer.targets.Length} {typeName} {settingsText}";

            return $"{GetName(target)} ({typeName}) {settingsText}";
        }

        static string GetName(IONode node) => string.IsNullOrEmpty(node.name) ? GetTextOrKey("runios-editor:gui.root") : node.name;

        void DrawBody()
        {
            if (drawer == null)
                return;

            BeginHierarchyMode();
            BeginWideMode();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(22);
                EditorGUILayout.BeginVertical();
                {
                    drawer.OnGUI(isDebug);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }
            EditorGUILayout.EndHorizontal();

            EndWideMode();
            EndHierarchyMode();
        }

        void DrawPreviewHeader()
        {
            if (drawer == null)
                return;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label(drawer.GetPreviewTitle(), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                drawer.OnPreviewSettings();
            }
            EditorGUILayout.EndHorizontal();
        }

        // Unity의 ObjectPreview.DrawPreview를 옮긴 것입니다. 배경은 드로어 대신 여기서 그립니다.
        void DrawPreview(IMGUIContainer container)
        {
            if (drawer == null)
                return;

            ImmutableArray<IONode> targets = drawer.targets;
            Rect previewArea = new Rect(Vector2.zero, container.contentRect.size);
            Event evt = Event.current;

            GUIStyle background = "PreBackground";
            if (evt.type == EventType.Repaint)
                background.Draw(previewArea, false, false, false, false);

            if (targets.Length == 1)
            {
                drawer.OnInteractivePreviewGUI(previewArea, targets[0], background);
                return;
            }

            // 배경 중 단색인 영역 안에 그립니다.
            Rect inner = previewGridPadding.Remove(previewArea);

            int maxRows = Mathf.Max(1, Mathf.FloorToInt((inner.height + gridSpacing) / (previewMinSize + gridSpacing + previewLabelHeight)));
            int maxColumns = Mathf.Max(1, Mathf.FloorToInt((inner.width + gridSpacing) / (previewMinSize + gridSpacing)));
            int neededCount = Mathf.Min(targets.Length, gridTargetCount);

            bool fixedSize = true;
            (int columns, int rows) division = (maxColumns, maxRows);
            if (neededCount < maxRows * maxColumns)
            {
                division = GetGridDivision(inner, neededCount, previewLabelHeight);
                fixedSize = false;
            }

            // 격자의 칸이 목표보다 조금 많을 수 있으므로, 대상이 더 있으면 남는 칸도 채웁니다.
            int count = Mathf.Min(division.columns * division.rows, targets.Length);

            // 간격 하나를 더하면 간격과 칸의 수가 같아져 계산이 간단해집니다.
            inner.width += gridSpacing;
            inner.height += gridSpacing;

            Vector2 cellSize = new Vector2(Mathf.FloorToInt((inner.width / division.columns) - gridSpacing), Mathf.FloorToInt((inner.height / division.rows) - gridSpacing));
            float previewSize = Mathf.Min(cellSize.x, cellSize.y - previewLabelHeight);
            if (fixedSize)
                previewSize = Mathf.Min(previewSize, previewMinSize);

            bool selectingOne = evt.type == EventType.MouseDown && evt.button == 0 && evt.clickCount == 2 && previewArea.Contains(evt.mousePosition);

            GUIStyle cellBackground = "PreBackgroundSolid";
            GUIStyle label = "PreMiniLabel";
            for (int i = 0; i < count; i++)
            {
                IONode node = targets[i];

                // ReSharper disable once PossibleLossOfFraction
                Rect r = new Rect(inner.x + (((i % division.columns) * inner.width) / division.columns), inner.y + (((i / division.columns) * inner.height) / division.rows), cellSize.x, cellSize.y);

                // 선택이 바뀌면 이 미리보기를 다시 만들 수 있으므로, 알린 뒤에는 이번 GUI를 끝냅니다.
                if (selectingOne && r.Contains(evt.mousePosition))
                {
                    previewTargetActivated?.Invoke(node);
                    GUIUtility.ExitGUI();
                }

                // 아래에 이름을 넣을 자리를 두고, 미리보기를 정사각형으로 만듭니다.
                r.height -= previewLabelHeight;
                Rect square = new Rect(r.x + ((r.width - previewSize) * 0.5f), r.y + ((r.height - previewSize) * 0.5f), previewSize, previewSize);

                // 칸 밖으로 그리지 않도록 그룹 안에 그립니다.
                GUI.BeginGroup(square);
                {
                    Rect cellRect = new Rect(0, 0, previewSize, previewSize);
                    if (evt.type == EventType.Repaint)
                        cellBackground.Draw(cellRect, false, false, false, false);

                    drawer.OnInteractivePreviewGUI(cellRect, node, cellBackground);
                }
                GUI.EndGroup();

                r.y = square.yMax;
                r.height = 16;
                GUI.Label(r, GetName(node), label);
            }

            if (evt.type == EventType.Repaint)
            {
                string text = string.Format(GetTextOrKey("runios-editor:pack_inspector.previewing"), count, targets.Length);
                float textHeight = ((GUIStyle)"PreOverlayLabel").CalcHeight(TempContent(text), previewArea.width);

                EditorGUI.DropShadowLabel(new Rect(previewArea.x, previewArea.yMax - textHeight - previewLabelPadding, previewArea.width, textHeight), text);
            }
        }

        // 칸이 minimumCount개 이상이면서 정사각형에 가장 가깝도록 열과 행의 수를 정합니다. Unity의 ObjectPreview.GetGridDivision을 옮긴 것입니다.
        static (int columns, int rows) GetGridDivision(Rect rect, int minimumCount, int labelHeight)
        {
            float approxSize = ((rect.width * rect.height) / minimumCount).Sqrt();
            int xCount = (rect.width / approxSize).FloorToInt();
            int yCount = (rect.height / (approxSize + labelHeight)).FloorToInt();

            while (xCount * yCount < minimumCount)
            {
                float ratioIfXInc = AbsRatioDiff((xCount + 1) / rect.width, yCount / (rect.height - (yCount * labelHeight)));
                float ratioIfYInc = AbsRatioDiff(xCount / rect.width, (yCount + 1) / (rect.height - ((yCount + 1) * labelHeight)));
                if (ratioIfXInc < ratioIfYInc)
                {
                    xCount++;
                    if (xCount * yCount > minimumCount)
                        yCount = ((float)minimumCount / xCount).CeilToInt();
                }
                else
                {
                    yCount++;
                    if (xCount * yCount > minimumCount)
                        xCount = ((float)minimumCount / yCount).CeilToInt();
                }
            }

            return (xCount, yCount);
        }

        static float AbsRatioDiff(float x, float y) => Max(x / y, y / x);
    }
}

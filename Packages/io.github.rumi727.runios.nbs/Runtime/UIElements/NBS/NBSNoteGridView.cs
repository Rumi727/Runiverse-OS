#nullable enable
using RuniOS.NBS;
using RuniOS.UIElements.Grids;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// Shows every note of a <see cref="NoteBlockClip"/> in a read-only <see cref="GridView"/>, one row per layer and one column per tick, like Note Block Studio.<br/>
    /// <see cref="GridView.corner"/> shows the time and length of the song, the tempo, and the bar, beat, and tick of the playhead, or of the start while there is none.<br/>
    /// While <see cref="player"/> plays, it draws the playhead and scrolls as <see cref="followMode"/> says. Clicking a ruler seeks; dragging on it moves the playhead and seeks where the drag ends.
    /// <br/><br/>
    /// <see cref="NoteBlockClip"/>의 모든 노트를 읽기 전용 <see cref="GridView"/>에 Note Block Studio처럼 레이어마다 행 하나, 틱마다 열 하나로 표시합니다.<br/>
    /// <see cref="GridView.corner"/>에는 플레이헤드 위치의 시간, 곡 길이, 템포, 마디·박·틱을 표시하며, 플레이헤드가 없으면 처음 위치를 표시합니다.<br/>
    /// <see cref="player"/>가 재생 중이면 플레이헤드를 그리고, <see cref="followMode"/>에 따라 스크롤합니다. 룰러를 클릭하면 재생 위치를 옮기고, 끌면 플레이헤드를 옮긴 뒤 끌기가 끝난 위치로 재생 위치를 옮깁니다.
    /// </summary>
    /// <remarks>
    /// The wheel scrolls the layers, Ctrl + wheel zooms, Shift + wheel scrolls the ticks, and Ctrl + Shift + wheel zooms the layers vertically.<br/>
    /// 휠은 레이어를 스크롤하고, Ctrl + 휠은 확대·축소하며, Shift + 휠은 틱 방향으로 스크롤하고, Ctrl + Shift + 휠은 레이어를 세로로 확대·축소합니다.
    /// </remarks>
    public partial class NBSNoteGridView : VisualElement
    {
        public const string ussClassName = "runios-nbs-note-grid-view";
        public const string gridUssClassName = ussClassName + "__grid";
        public const string timeUssClassName = ussClassName + "__time";
        public const string currentTimeUssClassName = ussClassName + "__current-time";
        public const string lengthUssClassName = ussClassName + "__length";
        public const string timeDetailsUssClassName = ussClassName + "__time-details";
        public const string timeBoxUssClassName = ussClassName + "__time-box";
        public const string layerHeaderUssClassName = ussClassName + "__layer-header";
        public const string layerHeaderLockedUssClassName = layerHeaderUssClassName + "--locked";
        public const string layerHeaderSoloUssClassName = layerHeaderUssClassName + "--solo";
        public const string layerHeaderMutedUssClassName = layerHeaderUssClassName + "--muted";
        public const string layerNumberUssClassName = ussClassName + "__layer-number";
        public const string layerNameUssClassName = ussClassName + "__layer-name";
        public const string layerLockUssClassName = ussClassName + "__layer-lock";
        public const string layerLockShackleUssClassName = layerLockUssClassName + "-shackle";
        public const string layerLockBodyUssClassName = layerLockUssClassName + "-body";
        public const string layerSoloUssClassName = ussClassName + "__layer-solo";
        public const string notesUssClassName = ussClassName + "__notes";
        public const string playheadUssClassName = ussClassName + "__playhead";
        public const string playheadMarkerUssClassName = ussClassName + "__playhead-marker";
        public const string effectOverlayUssClassName = ussClassName + "__effect-overlay";
        public const string accentUssClassName = ussClassName + "__accent";
        public const string savePopupUssClassName = ussClassName + "__save-popup";
        public const string stopperGuideUssClassName = ussClassName + "__stopper-guide";
        public const string lockedLayerShadeUssClassName = ussClassName + "__locked-layer-shade";

        /// <summary>
        /// The pixel size of one block at the default zoom, and the height of every layer at vertical scale <c>1</c>. Blocks and layers are <c>cellSize * grid.verticalScale</c> pixels high.<br/>
        /// 기본 배율에서 블록 하나의 픽셀 크기이자 세로 배율 <c>1</c>에서 모든 레이어의 높이입니다. 블록과 레이어의 높이는 <c>cellSize * grid.verticalScale</c> 픽셀입니다.
        /// </summary>
        public const float cellSize = 32;

        // Above this many visible notes, the notes are drawn as rectangles instead of blocks.
        const int maximumBlockCount = 1024;

        // Below this many pixels per tick, which is the width of a block, the notes are drawn as rectangles instead of blocks.
        const double minimumBlockPixelsPerTick = 4;

        static readonly CustomStyleProperty<float> mutedNoteOpacityProperty = new("--runios-nbs-muted-note-opacity");

        // The average colors of the block textures, used while the notes are drawn as rectangles.
        static readonly Color32[] denseColors =
        [
            new(0x00, 0x34, 0x67, 0xFF), new(0x19, 0x51, 0x20, 0xFF), new(0x73, 0x39, 0x3A, 0xFF), new(0x73, 0x73, 0x00, 0xFF),
            new(0x5C, 0x2D, 0x5A, 0xFF), new(0x3D, 0x1C, 0x13, 0xFF), new(0x73, 0x6E, 0x2E, 0xFF), new(0x73, 0x00, 0x73, 0xFF),
            new(0x27, 0x51, 0x5C, 0xFF), new(0x73, 0x73, 0x73, 0xFF), new(0x00, 0x53, 0x73, 0xFF), new(0x73, 0x07, 0x0A, 0xFF),
            new(0x73, 0x2B, 0x0A, 0xFF), new(0x00, 0x73, 0x00, 0xFF), new(0x73, 0x00, 0x2B, 0xFF), new(0x2C, 0x2C, 0x2C, 0xFF),
            new(0x74, 0x35, 0x26, 0xFF), new(0x69, 0x48, 0x33, 0xFF), new(0x3F, 0x58, 0x40, 0xFF), new(0x2C, 0x57, 0x47, 0xFF),
            new(0x7D, 0x49, 0x06, 0xFF), new(0x37, 0x6E, 0x15, 0xFF), new(0x7E, 0x23, 0x24, 0xFF), new(0x05, 0x6A, 0x68, 0xFF),
            new(0x66, 0x05, 0x59, 0xFF), new(0x06, 0x42, 0x78, 0xFF), new(0x75, 0x30, 0x3B, 0xFF), new(0x2C, 0x7E, 0x3C, 0xFF),
            new(0x54, 0x05, 0x74, 0xFF), new(0x62, 0x5F, 0x05, 0xFF), new(0x75, 0x05, 0x47, 0xFF), new(0x09, 0x73, 0x49, 0xFF),
            new(0x74, 0x37, 0x44, 0xFF), new(0x47, 0x3B, 0x48, 0xFF), new(0x3B, 0x77, 0x28, 0xFF), new(0x3E, 0x27, 0x7E, 0xFF),
            new(0x3B, 0x65, 0x48, 0xFF), new(0x44, 0x1B, 0x48, 0xFF)
        ];

        readonly NBSTickGridScale tickScale = new();
        readonly NBSTimeGridScale timeScale = new();
        readonly NBSTempoGridScale tempoScale = new();
        readonly Texture2D?[] textures = new Texture2D?[NBSNoteBlockElement.textureCount];
        readonly Stack<NBSNoteBlockElement> blockPool = new();
        readonly Func<GridSelection, VisualElement?> noteInfoPopupFactory;

        NBSMap? map;
        PlaneGridLayer? notesLayer;
        NBSStopperGuide? stopperGuide;

        // The opacity of the rectangles of muted layers, from USS, like the opacity that the muted class gives the blocks.
        float mutedNoteOpacity = 1;

        /// <summary>
        /// Initializes an empty view.<br/>
        /// 빈 뷰를 초기화합니다.
        /// </summary>
        public NBSNoteGridView()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            grid = new GridView { name = "grid", readOnly = true, headerWidth = 120 };
            grid.AddToClassList(gridUssClassName);
            grid.panZoom.zoomModifier = EventModifiers.Control;
            grid.panZoom.verticalModifier = EventModifiers.None;
            grid.panZoom.verticalZoomModifier = EventModifiers.Control | EventModifiers.Shift;

            // Layers can only get lower than cellSize, as the blocks can only get narrower, and stay at least one pixel high.
            grid.maximumVerticalScale = 1;
            grid.minimumVerticalScale = 1 / cellSize;
            grid.verticalScaleChanged += UpdateBlockHeights;

            grid.axis.maximumPixelsPerUnit = cellSize;
            grid.axis.pixelsPerUnit = cellSize;
            grid.axis.scales.Add(tickScale);
            grid.AddRuler(tickScale);
            grid.AddRuler(timeScale);
            grid.AddRuler(tempoScale);
            Add(grid);

            grid.axis.changed += () => stopperGuide?.MarkDirtyRepaint();

            noteInfoPopupFactory = selection => map != null ? NBSNoteInfoPopup.Create(map, selection) : null;
            grid.makeSelectionPopup = noteInfoPopupFactory;

            // The popup only shows information, so it stays enabled in the read-only grid.
            grid.selectionPopup.enabledWhenReadOnly = true;

            // Pointer input passes through the popup to the grid behind it. The popup content ignores picking too; see NBSNoteInfoPopup.
            grid.selectionPopup.pickingMode = PickingMode.Ignore;

            for (int i = 0; i < textures.Length; i++)
            {
                int index = i;
                this.AddManipulator(new AssetScopeManipulator<Texture2D>
                {
                    assetRef = $"runios:nbs/block/{index}",
                    applyAsset = x => SetTexture(index, x),
                    clearAsset = () => SetTexture(index, null)
                });
            }

            InitializePlayback();

            this.AddManipulator(new LocalizationManipulator(OnLanguageChanged));
        }

        /// <summary>
        /// Gets the grid that shows the notes.<br/>
        /// 노트를 표시하는 그리드를 가져옵니다.
        /// </summary>
        public GridView grid { get; }

        /// <summary>
        /// Gets or sets the clip to show, or <see langword="null"/> to show nothing. Setting it rebuilds the layers and scrolls to the start.<br/>
        /// 표시할 클립을 가져오거나 설정하며, 아무것도 표시하지 않으려면 <see langword="null"/>입니다. 설정하면 레이어를 다시 만들고 처음으로 스크롤합니다.
        /// </summary>
        public NoteBlockClip? clip
        {
            get;
            set
            {
                field = value;
                Rebuild(value != null ? value.map : null);
            }
        }

        void Rebuild(NBSMap? newMap)
        {
            map = newMap;

            grid.Clear();
            blockPool.Clear();
            notesLayer = null;
            stopperGuide = null;
            noteFlash.Clear();
            ResetPlayback();
            ResetTimeDisplay(newMap);

            tickScale.timeSignature = newMap?.header.timeSignature ?? 4;
            timeScale.tempo = newMap?.tempo;
            tempoScale.tempo = newMap?.tempo;

            if (newMap == null)
            {
                grid.axis.scrollRange = null;
                grid.ScheduleRefresh();
                return;
            }

            int layerCount = newMap.layers.Length;
            foreach (ref readonly NBSMapNote note in newMap.notes)
                layerCount = Math.Max(layerCount, note.layer + 1);

            PlaneGridItem[] items = new PlaneGridItem[newMap.notes.Count];
            int index = 0;
            foreach (ref readonly NBSMapNote note in newMap.notes)
            {
                NBSNoteGridItem item = new(note) { start = note.tick, top = note.layer };
                items[index++] = item;
                noteFlash.Add(item);
            }

            noteFlash.Sort();

            notesLayer = new PlaneGridLayer
            {
                unitHeight = cellSize,
                verticalLength = layerCount,
                maximumElementCount = maximumBlockCount,
                minimumElementPixelsPerUnit = minimumBlockPixelsPerTick,
                makeElement = MakeBlock,
                releaseElement = (_, element) => blockPool.Push((NBSNoteBlockElement)element),
                denseColor = GetDenseColor,
                makeRowHeader = MakeLayerHeader
            };
            notesLayer.AddToClassList(notesUssClassName);
            notesLayer.RegisterCallback<CustomStyleResolvedEvent>(OnNotesStyleResolved);
            notesLayer.items.AddRange(items);

            GridTrack track = grid.AddTrack(notesLayer);
            if (NBSLockedLayerShade.IsNeeded(newMap, layerCount))
                track.hierarchy.Insert(track.hierarchy.IndexOf(notesLayer), new NBSLockedLayerShade(newMap, layerCount, grid.headerWidth));

            if (newMap.stoppers.Length > 0)
            {
                stopperGuide = new NBSStopperGuide(grid.axis, newMap, layerCount, grid.headerWidth);
                track.hierarchy.Add(stopperGuide);
            }

            grid.axis.scrollRange = new GridRange(0, songLength);
            grid.axis.SetView(0, grid.axis.pixelsPerUnit);
            grid.ScheduleRefresh();
        }

        VisualElement MakeLayerHeader(int layer)
        {
            NBSMap currentMap = map!;

            VisualElement header = new() { name = "layer-header" };
            header.AddToClassList(layerHeaderUssClassName);
            header.EnableInClassList(layerHeaderMutedUssClassName, !currentMap.IsLayerAudible(layer));

            Label number = new((layer + 1).ToString()) { name = "number" };
            number.AddToClassList(layerNumberUssClassName);
            header.Add(number);

            if (currentMap.TryGetLayer(layer, out NBSLayer row))
            {
                header.EnableInClassList(layerHeaderLockedUssClassName, row.lockState == NBSLayerLock.locked);
                header.EnableInClassList(layerHeaderSoloUssClassName, row.lockState == NBSLayerLock.solo);

                Label name = new(row.name) { name = "name" };
                name.AddToClassList(layerNameUssClassName);
                header.Add(name);

                if (row.lockState == NBSLayerLock.locked)
                    header.Add(CreateLockBadge());
                else if (row.lockState == NBSLayerLock.solo)
                    header.Add(CreateSoloBadge());
            }

            return header;
        }

        // A padlock drawn from two elements, because there is no lock icon that runtime UI can load.
        static VisualElement CreateLockBadge()
        {
            VisualElement badge = new() { name = "lock" };
            badge.AddToClassList(layerLockUssClassName);
            SetLayerBadgeTooltip(badge);

            VisualElement shackle = new() { name = "shackle", pickingMode = PickingMode.Ignore };
            shackle.AddToClassList(layerLockShackleUssClassName);
            badge.Add(shackle);

            VisualElement body = new() { name = "body", pickingMode = PickingMode.Ignore };
            body.AddToClassList(layerLockBodyUssClassName);
            badge.Add(body);

            return badge;
        }

        static Label CreateSoloBadge()
        {
            Label badge = new("!") { name = "solo" };
            badge.AddToClassList(layerSoloUssClassName);
            SetLayerBadgeTooltip(badge);

            return badge;
        }

        // Other header children are left as they are, so the language change can pass every child of a header here.
        static void SetLayerBadgeTooltip(VisualElement element)
        {
            if (element.ClassListContains(layerLockUssClassName))
                element.tooltip = LocalizationUtility.GetTextOrKey("runios-editor:inspector.nbs_player.layer.locked");
            else if (element.ClassListContains(layerSoloUssClassName))
                element.tooltip = LocalizationUtility.GetTextOrKey("runios-editor:inspector.nbs_player.layer.solo");
        }

        // Texts that were read from the language are read again. Blocks on screen are bound again, and setting the factory again makes the popup of the selection again.
        void OnLanguageChanged()
        {
            effectOverlay.RefreshText();

            if (map != null && notesLayer != null)
            {
                foreach (VisualElement header in notesLayer.rowHeaders)
                {
                    for (int i = 0; i < header.childCount; i++)
                        SetLayerBadgeTooltip(header[i]);
                }

                foreach (GridItem item in notesLayer.visibleItems)
                {
                    if (item is NBSNoteGridItem noteItem && item.element is NBSNoteBlockElement block)
                        block.Bind(noteItem.note.note, map);
                }
            }

            grid.makeSelectionPopup = noteInfoPopupFactory;
        }

        VisualElement MakeBlock(PlaneGridItem item)
        {
            NBSNoteGridItem noteItem = (NBSNoteGridItem)item;
            NBSNote note = noteItem.note.note;
            NBSMap currentMap = map!;

            if (!blockPool.TryPop(out NBSNoteBlockElement? block))
                block = new NBSNoteBlockElement();

            // Blocks are pooled, so the classes of the layer are set on every reuse.
            int layer = noteItem.note.layer;
            block.EnableInClassList(NBSNoteBlockElement.mutedUssClassName, !currentMap.IsLayerAudible(layer));
            block.EnableInClassList(NBSNoteBlockElement.lockedUssClassName, currentMap.TryGetLayer(layer, out NBSLayer row) && row.lockState == NBSLayerLock.locked);

            block.style.height = blockHeight;
            block.Bind(note, currentMap);
            block.SetTexture(textures[NBSNoteBlockElement.GetTextureIndex(note.instrument)]);

            // A block made for a note that played a moment ago continues its flash.
            float flash = NBSNoteFlash.GetAmount(noteItem, Time.realtimeSinceStartupAsDouble);
            if (flash > 0)
                block.SetFlash(flash);

            return block;
        }

        Color GetDenseColor(PlaneGridItem item)
        {
            NBSMapNote note = ((NBSNoteGridItem)item).note;
            Color color = denseColors[NBSNoteBlockElement.GetTextureIndex(note.note.instrument)];
            return map!.IsLayerAudible(note.layer) ? color : color with { a = color.a * mutedNoteOpacity };
        }

        void OnNotesStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(mutedNoteOpacityProperty, out float opacity))
            {
                mutedNoteOpacity = opacity;
                notesLayer?.MarkDirtyRepaint();
            }
        }

        // The grid never sets element heights, so blocks follow the vertical scale here.
        float blockHeight => (float)(cellSize * grid.verticalScale);

        // Blocks already on screen take the new height; blocks made later get it in MakeBlock.
        // This runs on every wheel tick while zooming, so the items are walked by index instead of through an enumerator.
        void UpdateBlockHeights()
        {
            if (notesLayer == null)
                return;

            float height = blockHeight;
            IReadOnlyList<GridItem> items = notesLayer.visibleItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].element is NBSNoteBlockElement block)
                    block.style.height = height;
            }
        }

        // Blocks already on screen take a texture that finishes loading after them.
        void SetTexture(int index, Texture2D? texture)
        {
            textures[index] = texture;
            if (notesLayer == null)
                return;

            foreach (GridItem item in notesLayer.visibleItems)
            {
                if (item is NBSNoteGridItem noteItem && item.element is NBSNoteBlockElement block && NBSNoteBlockElement.GetTextureIndex(noteItem.note.note.instrument) == index)
                    block.SetTexture(texture);
            }
        }
    }
}

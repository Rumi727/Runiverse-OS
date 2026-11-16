#nullable enable
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using RuniOS.NBS;
using RuniOS.Sounds;
using RuniOS.UIElements.Grids;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// Shows every note of a <see cref="NoteBlockClip"/> in a read-only <see cref="GridView"/>, one track per layer and one column per tick, like Note Block Studio.<br/>
    /// While <see cref="player"/> plays, it draws the playhead and scrolls as <see cref="followMode"/> says. Clicking or dragging on a ruler seeks while playing.
    /// <br/><br/>
    /// <see cref="NoteBlockClip"/>의 모든 노트를 읽기 전용 <see cref="GridView"/>에 Note Block Studio처럼 레이어마다 트랙 하나, 틱마다 열 하나로 표시합니다.<br/>
    /// <see cref="player"/>가 재생 중이면 플레이헤드를 그리고 <see cref="followMode"/>에 따라 스크롤합니다. 재생 중에 룰러를 클릭하거나 끌면 재생 위치를 옮깁니다.
    /// </summary>
    /// <remarks>
    /// The wheel scrolls the layers, Ctrl + wheel zooms, and Shift + wheel scrolls the ticks.<br/>
    /// 휠은 레이어를 스크롤하고, Ctrl + 휠은 확대·축소하며, Shift + 휠은 틱 방향으로 스크롤합니다.
    /// </remarks>
    public class NBSNoteGridView : VisualElement
    {
        public const string ussClassName = "runios-nbs-note-grid-view";
        public const string gridUssClassName = ussClassName + "__grid";
        public const string layerHeaderUssClassName = ussClassName + "__layer-header";
        public const string layerHeaderLockedUssClassName = layerHeaderUssClassName + "--locked";
        public const string layerHeaderSoloUssClassName = layerHeaderUssClassName + "--solo";
        public const string layerNumberUssClassName = ussClassName + "__layer-number";
        public const string layerNameUssClassName = ussClassName + "__layer-name";
        public const string notesUssClassName = ussClassName + "__notes";
        public const string mutedNotesUssClassName = notesUssClassName + "--muted";
        public const string playheadUssClassName = ussClassName + "__playhead";
        public const string playheadMarkerUssClassName = ussClassName + "__playhead-marker";
        public const string accentUssClassName = ussClassName + "__accent";
        public const string savePopupUssClassName = ussClassName + "__save-popup";
        public const string stopperGuideUssClassName = ussClassName + "__stopper-guide";

        /// <summary>
        /// The pixel size of one block at the default zoom, and the height of every layer.<br/>
        /// 기본 배율에서 블록 하나의 픽셀 크기이자 모든 레이어의 높이입니다.
        /// </summary>
        public const float cellSize = 32;

        // How long a block takes to fall back to the alpha of its note after the note plays; Note Block Studio adds up to 0.5 to the alpha over one second.
        const double flashDuration = 1;

        // The song saved popup holds, then fades; the background accent is a faint tint of the accent color.
        const double savePopupHoldDuration = 1;
        const double savePopupFadeDuration = 0.25;
        const float accentBackgroundAlpha = 0.18f;
        static readonly Color defaultAccentColor = new Color32(0, 120, 212, 255);

        // The alpha of each pixel column of the wall that Note Block Studio draws at a sound stopper: it fades in towards the left edge of the tick and ends in two opaque columns.
        static readonly byte[] stopperGuideAlphas = [18, 41, 64, 100, 141, 192, 255, 255];

        static readonly CustomStyleProperty<Color> stopperGuideColorProperty = new("--runios-nbs-stopper-guide-color");

        // Above this many visible notes in one layer, the layer draws rectangles instead of blocks.
        const int maximumBlockCount = 128;

        // The average colors of the block textures, used while a layer draws rectangles.
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
        readonly GridItem playhead = new();
        readonly VisualElement playheadElement;
        readonly VisualElement playheadMarker;
        readonly VisualElement accentOverlay;
        readonly Label savePopup;
        readonly Texture2D?[] textures = new Texture2D?[NBSNoteBlockElement.textureCount];
        readonly Stack<NBSNoteBlockElement> blockPool = new();
        readonly List<NBSNoteGridItem> noteItems = new();
        readonly List<NBSNoteGridItem> flashingItems = new();
        readonly List<VisualElement> stopperGuides = new();

        readonly Func<GridSelection, VisualElement?> noteInfoPopupFactory;

        NBSMap? map;
        Color playheadColor = defaultAccentColor;
        Color stopperGuideColor = Color.black;
        double previousTime = double.NaN;
        double previousTick;
        double previousRealTime;
        double savePopupStartedAt = double.NegativeInfinity;

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
            grid.axis.maximumPixelsPerUnit = cellSize;
            grid.axis.pixelsPerUnit = cellSize;
            grid.axis.scales.Add(tickScale);
            grid.AddRuler(tickScale);
            grid.AddRuler(timeScale);
            grid.AddRuler(tempoScale);
            Add(grid);

            playheadElement = new VisualElement { name = "playhead", pickingMode = PickingMode.Ignore };
            playheadElement.AddToClassList(playheadUssClassName);
            playheadMarker = new VisualElement { name = "marker", pickingMode = PickingMode.Ignore };
            playheadMarker.AddToClassList(playheadMarkerUssClassName);
            playheadMarker.generateVisualContent += DrawPlayheadMarker;
            playheadElement.Add(playheadMarker);
            grid.overlay.defaultItemWidth = 2;
            grid.overlay.makeElement = _ => playheadElement;

            accentOverlay = new VisualElement { name = "accent", pickingMode = PickingMode.Ignore };
            accentOverlay.AddToClassList(accentUssClassName);
            accentOverlay.style.display = DisplayStyle.None;
            Add(accentOverlay);

            savePopup = new Label { name = "save-popup", pickingMode = PickingMode.Ignore };
            savePopup.AddToClassList(savePopupUssClassName);
            savePopup.style.display = DisplayStyle.None;
            Add(savePopup);

            grid.axis.changed += () =>
            {
                foreach (VisualElement guide in stopperGuides)
                    guide.MarkDirtyRepaint();
            };

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            noteInfoPopupFactory = selection => map != null ? NBSNoteInfoPopup.Create(map, selection) : null;
            grid.makeSelectionPopup = noteInfoPopupFactory;

            // The popup only shows information, so it stays enabled in the read-only grid.
            grid.selectionPopup.enabledWhenReadOnly = true;

            this.AddManipulator(new LocalizationManipulator(OnLanguageChanged));

            grid.selector.dragStarting += static args => args.ruler != null && args.button == (int)MouseButton.LeftMouse;
            grid.selector.dragging += Seek;
            grid.selector.clicking += args =>
            {
                if (args.ruler != null && args.button == (int)MouseButton.LeftMouse)
                    Seek(args);

                return false;
            };

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

            schedule.Execute(Update).Every(0);
        }

        /// <summary>
        /// Gets or sets how the view scrolls to keep the playhead in view while playing. It is shared by every view.<br/>
        /// 재생 중 플레이헤드를 화면에 두기 위해 뷰가 스크롤하는 방식을 가져오거나 설정합니다. 모든 뷰가 공유합니다.
        /// </summary>
        public static NBSFollowMode followMode { get; set; }

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
                Rebuild(value?.map);
            }
        }

        /// <summary>
        /// Gets or sets the player whose time the playhead follows, or <see langword="null"/> for no playhead. Its time is in seconds of <see cref="clip"/>.<br/>
        /// 플레이헤드가 따라갈 시간을 가진 플레이어를 가져오거나 설정하며, 플레이헤드가 없으면 <see langword="null"/>입니다. 시간은 <see cref="clip"/>의 초 단위입니다.
        /// </summary>
        public ISeekable? player { get; set; }

        void Rebuild(NBSMap? newMap)
        {
            map = newMap;

            grid.Clear();
            blockPool.Clear();
            noteItems.Clear();
            flashingItems.Clear();
            stopperGuides.Clear();
            ResetPlayback();

            tickScale.timeSignature = newMap?.header.timeSignature ?? 4;
            timeScale.tempo = newMap?.tempo;
            tempoScale.tempo = newMap?.tempo;

            if (newMap == null)
            {
                grid.axis.limit = null;
                grid.ScheduleRefresh();
                return;
            }

            int layerCount = newMap.layers.Length;
            foreach (ref readonly NBSMapNote note in newMap.notes)
                layerCount = Math.Max(layerCount, note.layer + 1);

            for (int layer = 0; layer < layerCount; layer++)
                AddLayer(newMap, layer);

            noteItems.Sort(static (x, y) => x.note.tick.CompareTo(y.note.tick));

            grid.axis.limit = new GridRange(0, Math.Max(1, newMap.tickLength));
            grid.axis.SetView(0, grid.axis.pixelsPerUnit);
            grid.ScheduleRefresh();
        }

        void AddLayer(NBSMap newMap, int layer)
        {
            NBSMap.LayerEnumerator notes = newMap.GetLayerNotes(layer);
            PlaneGridItem[] items = new PlaneGridItem[notes.count];

            int index = 0;
            foreach (ref readonly NBSMapNote note in notes)
                items[index++] = new NBSNoteGridItem(note) { start = note.tick };

            foreach (PlaneGridItem item in items)
                noteItems.Add((NBSNoteGridItem)item);

            PlaneGridLayer notesLayer = new()
            {
                unitHeight = cellSize,
                verticalLength = 1,
                maximumElementCount = maximumBlockCount,
                makeElement = MakeBlock,
                releaseElement = (_, element) => blockPool.Push((NBSNoteBlockElement)element),
                denseColor = static x => denseColors[NBSNoteBlockElement.GetTextureIndex(((NBSNoteGridItem)x).note.note.instrument)]
            };
            notesLayer.AddToClassList(notesUssClassName);
            notesLayer.EnableInClassList(mutedNotesUssClassName, !newMap.IsLayerAudible(layer));
            notesLayer.items.AddRange(items);

            GridTrack track = grid.AddTrack(notesLayer);
            track.header.AddToClassList(layerHeaderUssClassName);

            if (HasStopperTargeting(newMap, layer))
                AddStopperGuide(track, layer);

            Label number = new((layer + 1).ToString()) { name = "number" };
            number.AddToClassList(layerNumberUssClassName);
            track.header.Add(number);

            if (newMap.TryGetLayer(layer, out NBSLayer row))
            {
                track.header.EnableInClassList(layerHeaderLockedUssClassName, row.lockState == NBSLayerLock.locked);
                track.header.EnableInClassList(layerHeaderSoloUssClassName, row.lockState == NBSLayerLock.solo);

                Label name = new(row.name) { name = "name" };
                name.AddToClassList(layerNameUssClassName);
                track.header.Add(name);
            }
        }

        static bool HasStopperTargeting(NBSMap newMap, int layer)
        {
            foreach (NBSSoundStopper stopper in newMap.stoppers)
            {
                if (stopper.Targets(layer))
                    return true;
            }

            return false;
        }

        // The wall of a sound stopper is drawn over the blocks of every layer it stops, so it sits in an overlay after the layer in the track.
        void AddStopperGuide(GridTrack track, int layer)
        {
            VisualElement guide = new() { name = "stopper-guide", pickingMode = PickingMode.Ignore };
            guide.AddToClassList(stopperGuideUssClassName);
            guide.style.left = grid.headerWidth;
            guide.generateVisualContent += context => DrawStopperGuide(context, guide, layer);

            track.hierarchy.Add(guide);
            stopperGuides.Add(guide);
        }

        // Texts that were read from the language are read again. Blocks on screen are bound again, and setting the factory again makes the popup of the selection again.
        void OnLanguageChanged()
        {
            savePopup.text = LocalizationUtility.GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.song_saved");

            if (map != null)
            {
                foreach (GridTrack track in grid.tracks)
                {
                    foreach (GridItem item in track.layer.visibleItems)
                    {
                        if (item is NBSNoteGridItem noteItem && item.element is NBSNoteBlockElement block)
                            block.Bind(noteItem.note.note, map);
                    }
                }
            }

            grid.makeSelectionPopup = noteInfoPopupFactory;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(stopperGuideColorProperty, out Color color) && color != stopperGuideColor)
            {
                stopperGuideColor = color;
                foreach (VisualElement guide in stopperGuides)
                    guide.MarkDirtyRepaint();
            }
        }

        void DrawStopperGuide(MeshGenerationContext context, VisualElement guide, int layer)
        {
            if (map == null)
                return;

            ImmutableArray<NBSSoundStopper> stoppers = map.stoppers;
            GridAxis axis = grid.axis;
            GridRange range = axis.GetRange();

            // The wall reaches to the left of its tick, so a stopper just right of the view can still show.
            double margin = (stopperGuideAlphas.Length + 1) / axis.pixelsPerUnit;
            double startTick = range.start - 1;
            double endTick = range.end + margin;

            int low = 0;
            int high = stoppers.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (stoppers[middle].tick < startTick)
                    low = middle + 1;
                else
                    high = middle;
            }

            float height = guide.contentRect.height;
            List<(Rect rect, Color color)> rects = new();
            for (int i = low; i < stoppers.Length && stoppers[i].tick <= endTick; i++)
            {
                if (!stoppers[i].Targets(layer))
                    continue;

                // Note Block Studio draws the 16 pixel wall sprite with its origin at the tick minus one pixel; only its first eight pixels are not transparent.
                float left = MathF.Round(axis.ToPixel(stoppers[i].tick)) - 1 - stopperGuideAlphas.Length;
                for (int column = 0; column < stopperGuideAlphas.Length; column++)
                    rects.Add((new Rect(left + column + 1, 0, 1, height), stopperGuideColor with { a = stopperGuideAlphas[column] / 255f }));
            }

            context.DrawRects(CollectionsMarshal.AsSpan(rects));
        }

        VisualElement MakeBlock(PlaneGridItem item)
        {
            NBSNote note = ((NBSNoteGridItem)item).note.note;

            if (!blockPool.TryPop(out NBSNoteBlockElement? block))
                block = new NBSNoteBlockElement();

            block.style.height = cellSize;
            block.Bind(note, map!);
            block.SetTexture(textures[NBSNoteBlockElement.GetTextureIndex(note.instrument)]);

            // A block made for a note that played a moment ago continues its flash.
            double flash = 1 - ((Time.realtimeSinceStartupAsDouble - ((NBSNoteGridItem)item).playedAt) / flashDuration);
            if (flash > 0)
                block.SetFlash((float)flash);

            return block;
        }

        // Blocks already on screen take a texture that finishes loading after them.
        void SetTexture(int index, Texture2D? texture)
        {
            textures[index] = texture;

            foreach (GridTrack track in grid.tracks)
            {
                foreach (GridItem item in track.layer.visibleItems)
                {
                    if (item is NBSNoteGridItem noteItem && item.element is NBSNoteBlockElement block && NBSNoteBlockElement.GetTextureIndex(noteItem.note.note.instrument) == index)
                        block.SetTexture(texture);
                }
            }
        }

        void Update()
        {
            if (map == null || player is not { isPlaying: true } currentPlayer)
            {
                if (playhead.layer != null)
                    grid.overlay.items.Remove(playhead);

                ResetPlayback();
                return;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double time = currentPlayer.time;
            double tick = map.tempo.TimeToTick(time);

            // Playback moves forward about as fast as real time; anything else is a seek or a loop, which the effects and the flash must not treat as passing ticks.
            bool continuous = !double.IsNaN(previousTime) && time >= previousTime && time - previousTime <= ((now - previousRealTime) * 4) + 0.1;

            // In tick by tick mode the view moves by whole ticks, so the playhead stays on the tick instead of sliding between them.
            playhead.start = followMode == NBSFollowMode.tickByTick ? Math.Floor(tick) : tick;
            if (playhead.layer == null)
                grid.overlay.items.Add(playhead);

            Follow(tick);

            UpdateVisualEffects(tick, continuous, now);
            UpdateFlash(continuous ? previousTick : Math.Floor(tick) - 1, tick, now);

            previousTime = time;
            previousTick = tick;
            previousRealTime = now;
        }

        void UpdateVisualEffects(double tick, bool continuous, double now)
        {
            NBSVisualEffectMap effects = map!.visualEffects;
            NBSVisualEffectState state = effects.GetState(tick);

            Color accent = state.color is { } color ? (Color)color : defaultAccentColor;
            if (state.rainbow)
                accent = Color.HSVToRGB((float)(now * 0.25 % 1), 0.7f, 0.95f);

            playheadElement.style.backgroundColor = accent;
            if (playheadColor != accent)
            {
                playheadColor = accent;
                playheadMarker.MarkDirtyRepaint();
            }

            accentOverlay.style.display = state.backgroundAccent ? DisplayStyle.Flex : DisplayStyle.None;
            accentOverlay.style.backgroundColor = accent with { a = accentBackgroundAlpha };

            if (continuous && effects.TryFindSavePopup(previousTick, tick, out _))
                savePopupStartedAt = now;

            double elapsed = now - savePopupStartedAt;
            if (elapsed >= savePopupHoldDuration + savePopupFadeDuration)
            {
                savePopup.style.display = DisplayStyle.None;
                return;
            }

            savePopup.style.display = DisplayStyle.Flex;
            savePopup.style.opacity = elapsed <= savePopupHoldDuration ? 1 : (float)(1 - ((elapsed - savePopupHoldDuration) / savePopupFadeDuration));
        }

        // Notes on the ticks after fromTick up to toTick play now. Their blocks jump to fully opaque, then fall back to the alpha of the note.
        void UpdateFlash(double fromTick, double toTick, double now)
        {
            int low = 0;
            int high = noteItems.Count;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (noteItems[middle].note.tick <= fromTick)
                    low = middle + 1;
                else
                    high = middle;
            }

            for (int i = low; i < noteItems.Count && noteItems[i].note.tick <= toTick; i++)
            {
                // Like Note Block Studio, notes on locked layers do not flash.
                if (map!.TryGetLayer(noteItems[i].note.layer, out NBSLayer layer) && layer.lockState == NBSLayerLock.locked)
                    continue;

                noteItems[i].playedAt = now;
                flashingItems.Add(noteItems[i]);
            }

            for (int i = flashingItems.Count - 1; i >= 0; i--)
            {
                NBSNoteGridItem item = flashingItems[i];
                double flash = 1 - ((now - item.playedAt) / flashDuration);
                if (flash <= 0)
                {
                    (item.element as NBSNoteBlockElement)?.SetFlash(0);
                    flashingItems.RemoveAt(i);
                    continue;
                }

                (item.element as NBSNoteBlockElement)?.SetFlash((float)flash);
            }
        }

        void ResetPlayback()
        {
            previousTime = double.NaN;
            savePopupStartedAt = double.NegativeInfinity;
            accentOverlay.style.display = DisplayStyle.None;
            savePopup.style.display = DisplayStyle.None;

            foreach (NBSNoteGridItem item in flashingItems)
            {
                item.playedAt = double.NegativeInfinity;
                (item.element as NBSNoteBlockElement)?.SetFlash(0);
            }

            flashingItems.Clear();
        }

        void DrawPlayheadMarker(MeshGenerationContext context)
        {
            Rect rect = playheadMarker.contentRect;
            Painter2D painter = context.painter2D;
            painter.fillColor = playheadColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width * 0.5f, rect.height));
            painter.ClosePath();
            painter.Fill();
        }

        // The same rules as the marker of Note Block Studio. The axis limit keeps the view inside the song.
        void Follow(double tick)
        {
            GridAxis axis = grid.axis;
            double visibleTicks = axis.width / axis.pixelsPerUnit;
            if (!(visibleTicks > 0))
                return;

            double offset = followMode switch
            {
                NBSFollowMode.tickByTick => Math.Floor(tick) - Math.Ceiling(visibleTicks / 2),
                NBSFollowMode.frameByFrame => tick - (visibleTicks / 2),
                NBSFollowMode.pageByPage when Math.Floor(tick) >= axis.offset + visibleTicks - 1 || tick < axis.offset => Math.Floor(tick) - 1,
                _ => axis.offset
            };

            axis.SetView(offset, axis.pixelsPerUnit);
        }

        void Seek(GridPointerArgs args)
        {
            if (map == null || player == null)
                return;

            double time = map.tempo.TickToTime(Math.Max(0, args.position));
            if (player.isPlaying)
                player.time = time;
            else if (player is IPlayControl playControl)
                playControl.Play(time);
        }
    }
}

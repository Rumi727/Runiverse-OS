#nullable enable
using RuniOS.NBS;
using RuniOS.Sounds;
using RuniOS.UIElements.Grids;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    public partial class NBSNoteGridView
    {
        readonly GridItem playheadItem = new();
        readonly NBSPlayheadElement playhead = new();
        readonly NBSVisualEffectOverlay effectOverlay = new();
        readonly NBSNoteFlash noteFlash = new();

        // The previous frame of playback, to tell passing ticks from a seek or a loop.
        double previousTime = double.NaN;
        double previousTick;
        double previousRealTime;

        // The tick that a drag on a ruler moves the playhead to, or NaN while not dragging. The player seeks there once, when the drag ends.
        double dragTick = double.NaN;

        /// <summary>
        /// Gets or sets how the view scrolls to keep the playhead in view while playing. It is shared by every view.<br/>
        /// 재생 중 플레이헤드를 화면에 두기 위해 뷰가 스크롤하는 방식을 가져오거나 설정합니다. 모든 뷰가 공유합니다.
        /// </summary>
        public static NBSFollowMode followMode { get; set; }

        /// <summary>
        /// Gets or sets the player whose time the playhead follows, or <see langword="null"/> for no playhead. Its time is in seconds of <see cref="clip"/>.<br/>
        /// 플레이헤드가 따라갈 시간을 가진 플레이어를 가져오거나 설정하며, 플레이헤드가 없으면 <see langword="null"/>입니다. 시간은 <see cref="clip"/>의 초 단위입니다.
        /// </summary>
        public ISeekable? player { get; set; }

        // The scroll range of the song, which following keeps the view inside.
        double songLength => Math.Max(1, map!.tickLength);

        void InitializePlayback()
        {
            grid.overlay.defaultItemWidth = 2;
            grid.overlay.makeElement = _ => playhead;

            Add(effectOverlay);

            /*
             * Seeking on every move of a drag would fight with following: the drag position is measured from the view, which following moves to the seeked tick every frame,
             * so the next move would land on another tick. While dragging, only the playhead moves, and following waits.
             */
            grid.selector.dragStarting += args => map != null && player != null && IsSeekPress(args);
            grid.selector.dragging += args => dragTick = Math.Max(0, args.position);
            grid.selector.dragEnded += args =>
            {
                dragTick = double.NaN;
                Seek(Math.Max(0, args.position));
            };
            grid.selector.clicking += args =>
            {
                if (IsSeekPress(args))
                    Seek(Math.Max(0, args.position));

                return false;
            };

            schedule.Execute(Update).Every(0);
        }

        static bool IsSeekPress(GridPointerArgs args) => args.ruler != null && args.button == (int)MouseButton.LeftMouse;

        void Seek(double tick)
        {
            if (map == null || player == null)
                return;

            double time = map.tempo.TickToTime(tick);
            if (player.isPlaying)
                player.time = time;
            else if (player is IPlayControl playControl)
                playControl.Play(time);
        }

        void Update()
        {
            bool isPlaying = false;
            double tick = double.NaN;

            if (map != null && player is { isPlaying: true } playing)
            {
                isPlaying = true;
                double time = playing.time;
                tick = map.tempo.TimeToTick(time);
                UpdateEffects(map, time, tick);
            }
            else
                ResetPlayback();

            if (map != null && !double.IsNaN(dragTick))
                ShowPlayhead(dragTick);
            else if (isPlaying)
            {
                // In tick by tick mode the view moves by whole ticks, so the playhead stays on the tick instead of sliding between them.
                ShowPlayhead(followMode == NBSFollowMode.tickByTick ? Math.Floor(tick) : tick);
                Follow(tick);
            }
            else if (playheadItem.layer != null)
                grid.overlay.items.Remove(playheadItem);
        }

        void UpdateEffects(NBSMap currentMap, double time, double tick)
        {
            double now = Time.realtimeSinceStartupAsDouble;

            // Playback moves forward about as fast as real time; anything else is a seek or a loop, which the effects and the flash must not treat as passing ticks.
            bool continuous = !double.IsNaN(previousTime) && time >= previousTime && time - previousTime <= ((now - previousRealTime) * 4) + 0.1;

            playhead.SetColor(effectOverlay.Update(currentMap.visualEffects, previousTick, tick, continuous, now));
            noteFlash.Update(currentMap, continuous ? previousTick : Math.Floor(tick) - 1, tick, now);

            previousTime = time;
            previousTick = tick;
            previousRealTime = now;
        }

        void ResetPlayback()
        {
            previousTime = double.NaN;
            effectOverlay.Reset();
            noteFlash.Reset();
        }

        void ShowPlayhead(double tick)
        {
            playheadItem.start = tick;
            if (playheadItem.layer == null)
                grid.overlay.items.Add(playheadItem);
        }

        // The same rules as the marker of Note Block Studio, keeping the view inside the song as its scroll bar does.
        void Follow(double tick)
        {
            GridAxis axis = grid.axis;
            double visibleTicks = axis.width / axis.pixelsPerUnit;
            if (!(visibleTicks > 0))
                return;

            double offset;
            switch (followMode)
            {
                case NBSFollowMode.tickByTick:
                    offset = Math.Floor(tick) - Math.Ceiling(visibleTicks / 2);
                    break;
                case NBSFollowMode.frameByFrame:
                    offset = tick - (visibleTicks / 2);
                    break;
                case NBSFollowMode.pageByPage when Math.Floor(tick) >= axis.offset + visibleTicks - 1 || tick < axis.offset:
                    offset = Math.Floor(tick) - 1;
                    break;
                default:
                    return;
            }

            double lastOffset = songLength - visibleTicks;
            axis.SetView(lastOffset > 0 ? Math.Clamp(offset, 0, lastOffset) : 0, axis.pixelsPerUnit);
        }
    }
}

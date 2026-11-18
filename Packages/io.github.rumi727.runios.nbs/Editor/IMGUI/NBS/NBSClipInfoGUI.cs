#nullable enable
using RuniOS.NBS;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI.NBS
{
    /// <summary>
    /// Draws the information of a <see cref="NoteBlockClip"/> as read-only label fields, grouped into song, playback, content, and statistics sections.<br/>
    /// <see cref="NoteBlockClip"/>의 정보를 곡, 재생, 구성, 통계 섹션으로 나눠 읽기 전용 라벨 필드로 그립니다.
    /// </summary>
    public static class NBSClipInfoGUI
    {
        /// <summary>
        /// Draws the information of <paramref name="clip"/> with the layout system.<br/>
        /// 레이아웃 시스템으로 <paramref name="clip"/>의 정보를 그립니다.
        /// </summary>
        /// <param name="clip">
        /// The clip to describe.<br/>
        /// 정보를 표시할 클립입니다.
        /// </param>
        public static void DrawLayout(NoteBlockClip clip)
        {
            NBSHeader header = clip.header;
            NBSMap map = clip.map;

            DrawSection("runios-editor:gui.nbs.song");
            DrawField("runios-editor:gui.nbs.name", header.name);
            DrawField("runios-editor:gui.nbs.author", header.author);
            DrawField("runios-editor:gui.nbs.original_author", header.originalAuthor);
            DrawField("runios-editor:gui.nbs.imported_file_name", header.importedFileName);
            EditorGUILayout.LabelField(TrTempContent("runios-editor:gui.nbs.description"), new GUIContent(header.description), EditorStyles.wordWrappedLabel);

            Space();
            DrawSection("runios-editor:gui.nbs.playback");
            DrawField("runios-editor:gui.length", TimeUtility.ToTimeString(map.length));
            DrawField("runios-editor:gui.nbs.tick_length", map.tickLength);
            DrawField("runios-editor:gui.nbs.tempo", $"{header.ticksPerSecond:0.##} TPS · {header.ticksPerSecond * 15:0.##} BPM");
            DrawField("runios-editor:gui.nbs.time_signature", $"{header.timeSignature}/4");
            DrawField("runios-editor:gui.nbs.tempo_changes", map.tempo.changes.Length - 1);
            DrawToggle("runios-editor:gui.nbs.loop", header.loop);
            DrawField("runios-editor:gui.nbs.loop_start_tick", header.loopStartTick);
            DrawField("runios-editor:gui.nbs.max_loop_count", header.maxLoopCount == 0 ? "∞" : header.maxLoopCount);

            Space();
            DrawSection("runios-editor:gui.nbs.content");
            DrawField("runios-editor:gui.nbs.notes", map.notes.Count);
            DrawField("runios-editor:gui.nbs.columns", map.columns.Length);
            DrawField("runios-editor:gui.nbs.layers", map.layers.Length);
            DrawField("runios-editor:gui.nbs.sound_stoppers", map.stoppers.Length);
            DrawField("runios-editor:gui.nbs.custom_instruments", map.instruments.Length);
            DrawField("runios-editor:gui.nbs.version", header.version);

            Space();
            DrawSection("runios-editor:gui.nbs.statistics");
            DrawField("runios-editor:gui.nbs.minutes_spent", header.minutesSpent);
            DrawField("runios-editor:gui.nbs.left_clicks", header.leftClicks);
            DrawField("runios-editor:gui.nbs.right_clicks", header.rightClicks);
            DrawField("runios-editor:gui.nbs.blocks_added", header.blocksAdded);
            DrawField("runios-editor:gui.nbs.blocks_removed", header.blocksRemoved);
            DrawToggle("runios-editor:gui.nbs.auto_save", header.autoSave);
        }

        static void DrawSection(Identifier key) => GUILayout.Label(TrTempContent(key), RuniStyles.largeLabel);

        static void DrawField(Identifier key, object value) => EditorGUILayout.LabelField(TrTempContent(key), new GUIContent(value.ToString()));

        static void DrawToggle(Identifier key, bool value)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.Toggle(TrTempContent(key), value);
        }
    }
}

#nullable enable
using RuniOS.NBS;
using RuniOS.UIElements.Grids;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// Makes the content of the popup that <see cref="NBSNoteGridView"/> shows below the selected notes: everything the notes tell, from pitch, volume, and pan to the custom instrument and its sound file.<br/>
    /// <see cref="NBSNoteGridView"/>가 선택한 노트 아래에 표시하는 팝업의 내용을 만듭니다. 피치, 볼륨, 팬부터 커스텀 악기와 사운드 파일까지 노트가 가진 모든 정보입니다.
    /// </summary>
    public static class NBSNoteInfoPopup
    {
        public const string ussClassName = "runios-nbs-note-info";
        public const string rowUssClassName = ussClassName + "__row";
        public const string nameUssClassName = ussClassName + "__name";
        public const string valueUssClassName = ussClassName + "__value";

        /// <summary>
        /// Makes the popup content for <paramref name="selection"/>, or <see langword="null"/> when it holds no note.<br/>
        /// <paramref name="selection"/>의 팝업 내용을 만들며, 노트가 없으면 <see langword="null"/>입니다.
        /// </summary>
        /// <param name="map">
        /// The map of the notes.<br/>
        /// 노트가 속한 맵입니다.
        /// </param>
        /// <param name="selection">
        /// The selection of the grid.<br/>
        /// 그리드의 선택입니다.
        /// </param>
        public static VisualElement? Create(NBSMap map, GridSelection selection)
        {
            List<NBSMapNote> notes = [];
            foreach (GridItem item in selection)
            {
                if (item is NBSNoteGridItem noteItem)
                    notes.Add(noteItem.note);
            }

            if (notes.Count == 0)
                return null;

            VisualElement content = new();
            content.AddToClassList(ussClassName);

            if (notes.Count == 1)
                AddNote(content, map, notes[0]);
            else
                AddSummary(content, notes);

            return content;
        }

        static void AddNote(VisualElement content, NBSMap map, in NBSMapNote mapNote)
        {
            NBSNote note = mapNote.note;

            AddRow(content, "tick", note.tick.ToString());

            string layer = (note.layer + 1).ToString();
            if (map.TryGetLayer(note.layer, out NBSLayer row))
            {
                if (!string.IsNullOrEmpty(row.name))
                    layer += $" ({row.name})";

                if (row.lockState == NBSLayerLock.locked)
                    layer += $" · {GetText("locked")}";
                else if (row.lockState == NBSLayerLock.solo)
                    layer += $" · {GetText("solo")}";
            }

            AddRow(content, "layer", layer);
            AddRow(content, "time", $"{TimeUtility.ToTimeString(mapNote.time)} ({mapNote.time:0.###} s)");
            if (!double.IsPositiveInfinity(mapNote.nextStopTime))
                AddRow(content, "stop_time", $"{TimeUtility.ToTimeString(mapNote.nextStopTime)} ({mapNote.nextStopTime:0.###} s)");

            AddRow(content, "source_index", mapNote.sourceIndex.ToString());

            if (map.TryGetCustomInstrumentIndex(note.instrument, out int customIndex))
            {
                NBSInstrument instrument = map.instruments[customIndex];
                AddRow(content, "instrument_custom", $"{note.instrument} ({instrument.name})");
                AddRow(content, "sound_file", string.IsNullOrEmpty(instrument.soundFile) ? "-" : instrument.soundFile);
                AddRow(content, "instrument_key", $"{NBSNoteBlockElement.GetKeyName(instrument.key)} ({instrument.key})");
                AddRow(content, "press_key", instrument.pressKey ? "✓" : "✗");

                if (instrument.function != NBSInstrumentFunction.none)
                    AddRow(content, "function", instrument.function.ToString());
            }
            else if (note.instrument < NBSInstrument.vanillaCount)
                AddRow(content, "instrument", $"{note.instrument} ({GetText("built_in")})");
            else
                AddRow(content, "instrument", $"{note.instrument} ({GetText("missing")})");

            AddRow(content, "key", $"{NBSNoteBlockElement.GetKeyName(note.key)} ({note.key})");
            AddRow(content, "velocity", note.velocity.ToString());
            AddRow(content, "panning", GetPanningString(note.panning));
            AddRow(content, "pitch", note.pitch.ToString());
        }

        static void AddSummary(VisualElement content, List<NBSMapNote> notes)
        {
            AddRow(content, "notes", notes.Count.ToString());

            AddRange(content, "tick", notes, static x => x.note.tick);
            AddRange(content, "layer", notes, static x => x.note.layer + 1);
            AddRange(content, "key", notes, static x => x.note.key);
            AddRange(content, "velocity", notes, static x => x.note.velocity);
            AddRange(content, "panning", notes, static x => x.note.panning - 100);
            AddRange(content, "pitch", notes, static x => x.note.pitch);
        }

        static void AddRange(VisualElement content, string key, List<NBSMapNote> notes, Func<NBSMapNote, int> selector)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (NBSMapNote note in notes)
            {
                int value = selector(note);
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            AddRow(content, key, min == max ? min.ToString() : $"{min} ~ {max}");
        }

        // Note Block Studio stores the pan from 0 (left) to 200 (right), and 100 is the center.
        static string GetPanningString(int panning) => panning switch
        {
            100 => $"{GetText("center")} (100)",
            < 100 => $"{GetText("left")} {100 - panning} ({panning})",
            _ => $"{GetText("right")} {panning - 100} ({panning})"
        };

        static string GetText(string key) => LocalizationUtility.GetTextOrKey($"runios-editor:inspector.nbs_player.note_info.{key}");

        static void AddRow(VisualElement content, string key, string value)
        {
            VisualElement row = new();
            row.AddToClassList(rowUssClassName);

            Label name = new(GetText(key));
            name.AddToClassList(nameUssClassName);
            row.Add(name);

            Label valueLabel = new(value);
            valueLabel.AddToClassList(valueUssClassName);
            row.Add(valueLabel);

            content.Add(row);
        }
    }
}

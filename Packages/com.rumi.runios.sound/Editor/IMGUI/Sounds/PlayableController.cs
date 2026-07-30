#nullable enable
using RuniOS.Sounds;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public sealed class PlayableController
    {
        public PlayableController() => timeUnits = [];
        public PlayableController(params ITimeUnit[] timeUnits) => this.timeUnits = timeUnits.ToList();
        public PlayableController(IEnumerable<ITimeUnit> timeUnits) => this.timeUnits = timeUnits.ToList();

        public List<IPlayable> targets { get; } = [];

        [DisallowNull]
        public IPlayable? target
        {
            get => targets.Count == 1 ? targets[0] : null;
            set
            {
                targets.Clear();
                targets.Add(value);
            }
        }

        public List<ITimeUnit> timeUnits { get; }

        public void DrawLayout() => Draw(EditorGUILayout.GetControlRect(false, GetHeight()));

        public void Draw(Rect position)
        {
            float orgWidth = position.width;
            position.width = 150;

            DrawControl(position);

            position.x += position.width + 8;
            position.width = orgWidth - (position.width + 8);

            DrawSlider(position);
        }

        public void DrawControl(Rect position)
        {
            float orgX = position.x;
            float orgWidth = position.width;

            position.height = GetYSize(GUI.skin.button);
            position.width = (orgWidth - (3 * 2)) / 3;

            bool play = false;
            if (targets.Any(x => x.isPlaying))
            {
                if (GUI.Button(position, "▶↻"))
                    play = true;
            }
            else if (GUI.Button(position, "▶"))
                play = true;

            if (play)
            {
                foreach (var target in targets)
                    target.Play();
            }

            position.x += position.width + 3;

            if (targets.All(x => x.isPaused))
            {
                if (GUI.Button(position, "▶▮"))
                {
                    foreach (var target in targets)
                        target.UnPause();
                }
            }
            else if (GUI.Button(position, "▮▮"))
            {
                foreach (var target in targets)
                    target.Pause();
            }

            position.x += position.width + 3;

            if (GUI.Button(position, "■"))
            {
                foreach (var target in targets)
                    target.Stop();
            }

            position.x = orgX;
            position.width = orgWidth;

            position.y += position.height;
            position.y += EditorGUIUtility.standardVerticalSpacing;

            BeginLabelWidth(75);
            for (int i = 0; i < timeUnits.Count; i++)
            {
                ITimeUnit timeUnit = timeUnits[i];
                position.height = timeUnit.GetHeight();

                timeUnit.DrawField(position, targets);

                position.y += position.height;
                position.y += EditorGUIUtility.standardVerticalSpacing;
            }
            EndLabelWidth();
        }

        public void DrawSlider(Rect position)
        {
            position.height = GetYSize(GUI.skin.button);

            EditorGUI.showMixedValue = target == null || !target.isPlaying;

            EditorGUI.BeginChangeCheck();
            float sliderValue = GUI.HorizontalSlider(position, (target?.time ?? 0).ClampToFloat(), 0, (target?.length ?? 0).ClampToFloat());
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < targets.Count; i++)
                    targets[i].time = sliderValue;
            }

            EditorGUI.showMixedValue = false;

            position.y += position.height;
            position.y += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < timeUnits.Count; i++)
            {
                ITimeUnit timeUnit = timeUnits[i];
                position.height = timeUnit.GetHeight();

                BeginFontSize(11, RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleLeft, RuniStyles.richLabel);
                GUI.Label(position, RichNumberMSpace(timeUnit.TimeToString(target)), RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleCenter, RuniStyles.richLabel);
                GUI.Label(position, RichNumberMSpace(timeUnit.RemainingTimeToString(target)), RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleRight, RuniStyles.richLabel);
                GUI.Label(position, RichNumberMSpace(timeUnit.LengthToString(target)), RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                EndFontSize(RuniStyles.richLabel);

                position.y += position.height;
                position.y += EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public float GetHeight()
        {
            float height = GetYSize(GUI.skin.button);
            height += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < timeUnits.Count; i++)
            {
                height += timeUnits[i].GetHeight();
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
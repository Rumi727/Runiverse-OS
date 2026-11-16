#nullable enable
using RuniOS.NBS;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    // Covers NBSNoteGridView with the visual effects of the song while it plays: the background accent, a faint tint of the accent color, and the song saved popup.
    // The accent color, which the rainbow effect cycles, is returned so that the playhead takes it too.
    internal sealed class NBSVisualEffectOverlay : VisualElement
    {
        public static readonly Color defaultAccentColor = new Color32(0, 120, 212, 255);

        const float backgroundAlpha = 0.18f;

        // The song saved popup holds, then fades.
        const double savePopupHoldDuration = 1;
        const double savePopupFadeDuration = 0.25;

        readonly Label savePopup;
        double savePopupStartedAt = double.NegativeInfinity;

        public NBSVisualEffectOverlay()
        {
            name = "accent";
            pickingMode = PickingMode.Ignore;
            AddToClassList(NBSNoteGridView.accentUssClassName);

            savePopup = new Label { name = "save-popup", pickingMode = PickingMode.Ignore };
            savePopup.AddToClassList(NBSNoteGridView.savePopupUssClassName);
            savePopup.style.display = DisplayStyle.None;
            Add(savePopup);
        }

        public void RefreshText() => savePopup.text = LocalizationUtility.GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.song_saved");

        // Called every frame while playing. A popup between the ticks shows only when playback passed them; continuous is false after a seek or a loop.
        public Color Update(NBSVisualEffectMap effects, double previousTick, double tick, bool continuous, double now)
        {
            NBSVisualEffectState state = effects.GetState(tick);

            Color accent = state.color is { } color ? (Color)color : defaultAccentColor;
            if (state.rainbow)
                accent = Color.HSVToRGB((float)(now * 0.25 % 1), 0.7f, 0.95f);

            style.backgroundColor = state.backgroundAccent ? accent with { a = backgroundAlpha } : Color.clear;

            if (continuous && effects.TryFindSavePopup(previousTick, tick, out _))
                savePopupStartedAt = now;

            double elapsed = now - savePopupStartedAt;
            bool shown = elapsed < savePopupHoldDuration + savePopupFadeDuration;
            savePopup.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (shown)
                savePopup.style.opacity = elapsed <= savePopupHoldDuration ? 1 : (float)(1 - ((elapsed - savePopupHoldDuration) / savePopupFadeDuration));

            return accent;
        }

        public void Reset()
        {
            savePopupStartedAt = double.NegativeInfinity;
            style.backgroundColor = Color.clear;
            savePopup.style.display = DisplayStyle.None;
        }
    }
}

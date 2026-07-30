#nullable enable
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.Linq;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    public abstract class RuniAudioSourceEditor<TTarget> : CustomInspectorBase<TTarget> where TTarget : RuniAudioSource
    {
        public PlayableController playableController { get; } = new PlayableController(new GenericTimeUnit());

        protected override void OnEnable()
        {
            base.OnEnable();
            playableController.targets.SyncWithEnumerable(targets.WhereNotNull());
        }

        public override void OnInspectorGUI()
        {
            playableController.DrawLayout();

            if (targets.WhereNotNull().Any(x => x.isPlaying && !x.isPaused))
                Repaint();
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RuniAudioSource), true)]
    class RuniAudioSourceDefaultEditor : RuniAudioSourceEditor<RuniAudioSource>;
}
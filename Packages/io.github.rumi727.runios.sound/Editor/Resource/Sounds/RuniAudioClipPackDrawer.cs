#nullable enable
using RuniOS.Resource;
using RuniOS.Sounds;
using System.Collections.Immutable;

namespace RuniOS.Editor.Resource.Sounds
{
    public abstract class RuniAudioClipPackDrawer<T>(ImmutableArray<PackDrawer.PathPair> targets) : PackDrawer(targets) where T : RuniAudioClip
    {
        public override string targetTypeName => typeof(T).GetTypeDisplayName();

        public abstract T? targetClip { get; }

        protected override void OnGUI(bool isDebug = false)
        {
            if (DrawSettingLayout())
                Space();

            GUILayout.Label(TrTempContent("gui.info"), RuniStyles.largeLabel);
            DrawInfoLayout();
        }

        protected virtual bool DrawSettingLayout() => false;

        protected virtual void DrawInfoLayout() => DrawInfoFieldLayout("runios-editor:gui.length", targetClip != null ? targetClip.length : "―");

        protected static void DrawInfoFieldLayout(Identifier key, object value) => EditorGUILayout.LabelField(TrTempContent(key), new GUIContent(value.ToString()));
    }
}
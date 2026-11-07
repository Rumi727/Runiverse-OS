#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Localizations;
using System.Collections.Immutable;

namespace RuniOS.Editor.Resource
{
    public sealed class LanguagePackDrawer(ImmutableArray<IONode> targets) : PackDrawer(targets)
    {
        public override string targetTypeName => typeof(LocalizationData).GetTypeDisplayName();

        // 불러오기 전에는 비어 있습니다.
        GUIContent[] contents = [];

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.Any() && relativePaths.All(x => IsMatch(x, "lang", IPatternMatcher.jsonMatcher));

        protected internal override async UniTask OnEnable()
        {
            GUIContent[] loadedContents = new GUIContent[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                loadedContents[i] = new GUIContent(await targets[i].file.ReadAllText());

            contents = loadedContents;
            Repaint();
        }

        protected internal override void OnGUI(bool isDebug = false)
        {
            if (targets.Length != 1 || contents.Length != 1)
                return;

            GUIStyle style = "ScriptText";
            Rect position = GUILayoutUtility.GetRect(contents[0], style);
            EditorGUI.SelectableLabel(position, contents[0].text, style);
        }
    }
}
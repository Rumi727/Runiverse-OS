#nullable enable
using RuniOS.IO;
using RuniOS.Localizations;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;

namespace RuniOS.Editor.Resource
{
    public sealed class LanguagePackDrawer : PackDrawer
    {
        public LanguagePackDrawer(ImmutableArray<PathPair> targets) : base(targets)
        {
            contents =
            [
                ..targets
                    .Select(x => x.rootPath / x.relativePath)
                    .Select(x => x.value)
                    .Select(File.ReadAllText)
                    .Select(x => new GUIContent(x))
            ];
        }

        public override string targetTypeName => typeof(LocalizationData).GetTypeDisplayName();

        readonly GUIContent[] contents = [];

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => Regex.IsMatch(x.value, "^assets/.*/lang/.*\\.json$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));

        protected internal override void OnGUI(bool isDebug = false)
        {
            if (targets.Length == 1)
                return;

            GUIStyle style = "ScriptText";
            Rect position = GUILayoutUtility.GetRect(contents[0], style);
            EditorGUI.SelectableLabel(position, contents[0].text, style);
        }
    }
}
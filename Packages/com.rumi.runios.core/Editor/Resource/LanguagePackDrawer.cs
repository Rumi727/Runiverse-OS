#nullable enable
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Localizations;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;

namespace RuniOS.Editor.Resource
{
    public sealed class LanguagePackDrawer(PhysicalPath rootPath, ImmutableArray<RuniPath> relativePaths) : PackDrawer(rootPath, relativePaths)
    {
        public override string targetTypeName => typeof(LocalizationData).GetTypeDisplayName();

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => Regex.IsMatch(x.value, "^assets/.*/lang/.*\\.json$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));

        protected internal override void OnEnable()
        {
            contents =
            [
                ..relativePaths
                    .Select(x => rootPath / x)
                    .Select(x => x.value)
                    .Select(File.ReadAllText)
                    .Select(x => new GUIContent(x))
            ];
        }

        GUIContent[] contents = [];

        protected internal override void OnGUI(bool isDebug = false)
        {
            if (relativePaths.TwoOrMore())
                return;
            
            GUIStyle style = "ScriptText";
            Rect position = GUILayoutUtility.GetRect(contents[0], style);
            EditorGUI.SelectableLabel(position, contents[0].text, style);
        }
    }
}
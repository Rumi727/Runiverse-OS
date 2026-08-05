#nullable enable
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Localizations;
using System.IO;
using System.Text.RegularExpressions;

namespace RuniOS.Editor.Resource
{
    public sealed class LanguagePackDrawer : PackDrawer
    {
        public override string targetTypeName => typeof(LocalizationData).GetTypeDisplayName();

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => Regex.IsMatch(x.value, "^assets/.*/lang/.*\\.json$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));

        protected internal override void OnEnable(PhysicalPath rootPath, IReadOnlyList<RuniPath> relativePaths)
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

        protected internal override void OnGUI(PhysicalPath rootPath, IReadOnlyList<RuniPath> relativePaths, bool isDebug = false)
        {
            if (relativePaths.TwoOrMore())
                return;
            
            GUIStyle style = "ScriptText";
            Rect position = GUILayoutUtility.GetRect(contents[0], style);
            EditorGUI.SelectableLabel(position, contents[0].text, style);
        }
    }
}
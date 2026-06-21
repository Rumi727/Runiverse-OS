#nullable enable
using RuniOS.IO;
using RuniOS.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace RuniOS.Editor.Resource
{
    public sealed class LanguagePackDrawer : PackDrawer
    {
        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => Regex.IsMatch(x.value, "^assets/.*/lang/.*\\.json$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));

        public override void OnEnable(IEnumerable<RuniPath> relativePaths)
        {
            contents =
            [
                ..relativePaths
                    .Select(x => (PhysicalPath)Application.streamingAssetsPath / x)
                    .Select(x => x.value)
                    .Select(File.ReadAllText)
                    .Select(x => new GUIContent(x))
            ];
        }

        GUIContent[] contents = [];
        
        public override void OnGUI(IEnumerable<RuniPath> relativePaths, bool isDebug = false)
        {
            if (relativePaths.TwoOrMore())
                return;
            
            GUIStyle style = "ScriptText";
            Rect position = GUILayoutUtility.GetRect(contents[0], style);
            EditorGUI.SelectableLabel(position, contents[0].text, style);
        }
    }
}
#nullable enable
using RuniOS.IO;
using RuniOS.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace RuniOS.Editor.Resource
{
    public sealed class LanguagePackDrawer : PackDrawer
    {
        public override bool IsMatch(IEnumerable<FilePath> relativePaths) => relativePaths.All(x => Regex.IsMatch(x, "^assets/.*/lang/.*\\.json$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));

        public override void OnEnable(IEnumerable<FilePath> relativePaths)
        {
            contents = relativePaths
                .Select(x => (Application.streamingAssetsPath + x).value)
                .Select(File.ReadAllText)
                .Select(x => new GUIContent(x))
                .ToArray();
        }

        GUIContent[] contents = Array.Empty<GUIContent>();
        
        public override void OnGUI(IEnumerable<FilePath> relativePaths, bool isDebug = false)
        {
            if (relativePaths.TwoOrMore())
                return;
            
            GUIStyle style = "ScriptText";
            Rect position = GUILayoutUtility.GetRect(contents[0], style);
            EditorGUI.SelectableLabel(position, contents[0].text, style);
        }
    }
}
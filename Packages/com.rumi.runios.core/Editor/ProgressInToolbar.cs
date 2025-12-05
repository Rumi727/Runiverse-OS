using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.APIMarshal.UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace RuniOS.Editor
{
    /// <summary>
    /// 에디터 상단 툴바의 프로그래스 바를 관리합니다.
    /// </summary>
    public static class ProgressInToolbar
    {
        [MainToolbarElement("RuniOS/Progress Bar", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement ProgressBarElement() => new MainToolbarProgress();
        
        class MainToolbarProgress : MainToolbarElementMarshal
        {
            public override VisualElement CreateElementMarshal() => new IMGUIContainer(OnToolbarGUI);
        }

        static readonly Dictionary<string, Dictionary<string, float>> progresses = new();
        static GUIViewBridge? toolbarGUIView;

        static void OnToolbarGUI()
        {
            toolbarGUIView = GUIViewBridge.current;
            
            foreach (var progress in progresses)
            {
                if (progress.Value.Count <= 0)
                    continue;
                
                Rect rect = GUILayoutUtility.GetRect(120, 20, GUILayout.ExpandWidth(false));
                EditorGUI.ProgressBar(rect, progress.Value.Sum(x => x.Value) / progress.Value.Count, GetTextOrKey(progress.Key));
            }
        }

        /// <summary>
        /// 프로그레스 바의 진행도를 설정합니다.
        /// </summary>
        /// <param name="progressText">프로그레스 바에 표시할 텍스트</param>
        /// <param name="id">한 프로그레스 바에서 여러개의 진행도를 구분할 고유 id</param>
        /// <param name="value">0에서 1 사이의 진행도</param>
        public static void SetProgress(string progressText, string id, float value)
        {
            if (!progresses.TryGetValue(progressText, out Dictionary<string, float>? progress))
                progress = progresses[progressText] = new Dictionary<string, float>();

            if (value < 1)
                progress[id] = value;
            else
                progress.Remove(id);
            
            toolbarGUIView?.Repaint();
        }
    }
}
#nullable enable
#if UNITY_EDITOR
using RuniOS.UIElements.IO;
using UnityEditor;
using UnityEngine.UIElements;

/// <summary>
/// UI Builder에서는 우클릭 등의 입력을 캔버스가 가져가므로, 에디터 창에서 IO 브라우저를 직접 테스트합니다.
/// </summary>
public sealed class IOBrowserTestWindow : EditorWindow
{
    [MenuItem("RuniOS/IO Browser Test")]
    static void Open() => GetWindow<IOBrowserTestWindow>("IO Browser Test");

    void CreateGUI() => rootVisualElement.Add(new FileSystemBrowser { style = { flexGrow = 1 } });
}
#endif

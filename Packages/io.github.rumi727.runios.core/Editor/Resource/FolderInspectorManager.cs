#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;

namespace RuniOS.Editor.Resource
{
    /// <summary>
    /// 폴더 뷰(선택 안함) 상태일 때 표시할 "가짜 에디터"의 생명주기를 관리
    /// </summary>
    [InitializeOnLoad]
    static class FolderInspectorManager
    {
        static DefaultAssetHook? cachedEditor;
        static DefaultAsset? dummyTarget;

        static FolderInspectorManager()
        {
            InspectorHook.onGUI += OnBridgeGUI;
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        static void OnBridgeGUI(InspectorWindowBridge instance)
        {
            bool isDebug = PropertyEditorBridge.__GetInstanceFrom(instance.__instance).inspectorMode != InspectorMode.Normal;

            // 선택된 객체가 있거나, 폴더 뷰 모드가 아니면 무시
            if (!isDebug && (Selection.objects.Length > 0 || !PackInspectorSystem.isFolderViewMode))
            {
                Cleanup(); // 상태가 바뀌었으면 정리
                return;
            }

            // --- Shadow Editor 생성 및 관리 로직 ---
            if (dummyTarget == null)
            {
                // 현재 보고 있는 폴더를 타겟으로 로드
                RuniPath path = PackInspectorSystem.packRootPath / PackInspectorSystem.activeFolderPath;
                dummyTarget = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path.value);
            }

            if (cachedEditor == null && dummyTarget != null)
            {
                // 강제로 Editor 인스턴스 생성
                UnityEditor.Editor? editor = null;
                UnityEditor.Editor.CreateCachedEditor(dummyTarget, typeof(DefaultAssetHook), ref editor);

                cachedEditor = editor as DefaultAssetHook;
            }

            if (cachedEditor == null)
                return;

            cachedEditor.DrawHeader();

            BeginHierarchyMode();
            BeginWideMode();

            cachedEditor.OnInspectorGUI();

            EndWideMode();
            EndHierarchyMode();
        }

        static void Cleanup()
        {
            if (cachedEditor != null)
            {
                Object.DestroyImmediate(cachedEditor);
                cachedEditor = null;
            }

            dummyTarget = null;
        }
    }
}
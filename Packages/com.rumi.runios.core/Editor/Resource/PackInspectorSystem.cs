#nullable enable
using System.Collections.Immutable;
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor.Resource
{
    // TODO: activeDrawer, activePaths 시스템 제거. (에디터 인스턴스의 타겟 오브젝트로 관리하기)
    public static partial class PackInspectorSystem
    {
        public static readonly RuniPath packRootPath;
        
        static ImmutableArray<PackDrawer> drawers;

        public static PackDrawer? activeDrawer { get; private set; }
        public static ImmutableArray<RuniPath> activePaths { get; private set; } = ImmutableArray<RuniPath>.Empty;

        public static bool isFolderViewMode { get; private set; }
        public static RuniPath activeFolderPath { get; private set; }

        public static event Action? onActiveDrawerChanged;

        static PackInspectorSystem() => packRootPath = PhysicalPath.From(Application.streamingAssetsPath).GetRelativePath(projectPath);

        [OnCodeInitializing]
        static void OnCodeInitializing()
        {
            ReflectionUtility.onListUpdate += UpdateDrawers;
            UpdateDrawers();

            static void UpdateDrawers()
            {
                drawers =
                [
                    ..ReflectionUtility.types
                        .Where(x => !x.IsAbstract && !x.IsConstructedGenericType && x.IsSubclassOf(typeof(PackDrawer)))
                        .Select(x =>
                        {
                            try
                            {
                                return Activator.CreateInstance(x, (PhysicalPath)Application.streamingAssetsPath, ImmutableArray<RuniPath>.Empty);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }

                            return null;
                        })
                        .WhereNotNull()
                        .Cast<PackDrawer>()
                        .OrderByDescending(x => x.order)
                ];
            }

            Selection.selectionChanged += RefreshState;
            EditorApplication.update += CheckFolderChange;
        }

        [OnCodeDeinitializing]
        static void OnCodeDeinitializing()
        {
            Selection.selectionChanged -= RefreshState;
            EditorApplication.update -= CheckFolderChange;

            activeDrawer?.OnDisable();
            activeDrawer = null;
        }

        public static bool TryGetRelativePathFrom(Object obj, out RuniPath path)
        {
            path = RuniPath.From(AssetDatabase.GetAssetPath(obj));
            if (path.TryGetRelativePath(packRootPath, out path))
                return true;

            return false;
        }

        static void CheckFolderChange() => CheckFolder(false);

        static void RefreshState()
        {
            // 1. 파일 선택 시
            if (Selection.objects.Length > 0)
            {
                var paths = Selection.objects
                    .Select(AssetDatabase.GetAssetPath)
                    .Select(RuniPath.From)
                    .Select(x => 
                    {
                        bool success = x.TryGetRelativePath(packRootPath, out RuniPath path);
                        return (success, path);
                    })
                    .Where(x => x.success)
                    .Select(x => x.path);

                UpdateDrawer(paths, false);
            }
            else
                CheckFolder(true);
        }

        static RuniPath lastCheckPath = RuniPath.empty;
        static void CheckFolder(bool force)
        {
            if (Selection.objects.Length > 0)
                return;

            RuniPath currentPath = (RuniPath)ProjectWindowUtilBridge.GetActiveFolderPath();
            if (force || lastCheckPath != currentPath)
            {
                lastCheckPath = currentPath;
                InspectorWindowBridge.RepaintAllInspectors();

                if (currentPath.TryGetRelativePath(packRootPath, out RuniPath relative))
                {
                    activeFolderPath = relative;
                    UpdateDrawer(Enumerable.Repeat(relative, 1), true);
                }
                else
                {
                    activeFolderPath = RuniPath.empty;
                    UpdateDrawer([], true);
                }
            }
        }

        static void UpdateDrawer(IEnumerable<RuniPath> paths, bool isFolderView)
        {
            isFolderViewMode = isFolderView;
            
            if (paths.IsEmpty())
            {
                SetNewDrawer(null, []);
                return;
            }

            PackDrawer? drawer = drawers
                .Where(x => x.IsMatch(paths))
                .Select(x => (PackDrawer)Activator.CreateInstance(x.GetType(), (PhysicalPath)Application.streamingAssetsPath, paths.ToImmutableArray()))
                .FirstOrDefault();

            SetNewDrawer(drawer, paths);
        }

        static void SetNewDrawer(PackDrawer? drawer, IEnumerable<RuniPath> paths)
        {
            if (activeDrawer != null)
            {
                activeDrawer.OnDisable();
                activeDrawer.isEnabled = false;
            }

            activeDrawer = drawer;
            activePaths = [..paths];

            if (activeDrawer != null)
            {
                activeDrawer.isEnabled = true;
                activeDrawer.OnEnable();
            }

            onActiveDrawerChanged?.Invoke();
        }
    }
}
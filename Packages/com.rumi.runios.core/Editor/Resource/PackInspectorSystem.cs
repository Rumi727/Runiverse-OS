#nullable enable
using System.Collections.Immutable;
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Reflection;

namespace RuniOS.Editor.Resource
{
    [InitializeOnLoad]
    public static class PackInspectorSystem
    {
        public static readonly FilePath packRootPath;
        
        static readonly ImmutableArray<PackDrawer> drawers;

        public static PackDrawer? activeDrawer { get; private set; }
        public static ImmutableArray<FilePath> activePaths { get; private set; } = ImmutableArray<FilePath>.Empty;

        public static bool isFolderViewMode { get; private set; }
        public static FilePath activeFolderPath { get; private set; }

        public static event Action? onActiveDrawerChanged;

        static PackInspectorSystem()
        {
            packRootPath = Application.streamingAssetsPath.ToPath().TrimStartPath(projectPath);
            drawers = ReflectionUtility.types
                .Where(x => x.HasDefaultConstructor() && x.IsSubclassOf(typeof(PackDrawer)))
                .Select(x => (PackDrawer)Activator.CreateInstance(x))
                .OrderByDescending(x => x.order)
                .ToImmutableArray();

            Selection.selectionChanged += RefreshState;
            EditorApplication.update += CheckFolderChange;
            RefreshState();
        }

        static void CheckFolderChange() => CheckFolder(false);

        static void RefreshState()
        {
            // 1. 파일 선택 시
            if (Selection.objects.Length > 0)
            {
                var paths = Selection.objects
                    .Select(AssetDatabase.GetAssetPath)
                    .Select(x => 
                    {
                        bool success = x.ToPath().TryTrimStartPath(packRootPath, out FilePath path);
                        return (success, path);
                    })
                    .Where(x => x.success)
                    .Select(x => x.path);

                UpdateDrawer(paths, false);
            }
            else
                CheckFolder(true);
        }

        static string lastCheckPath = string.Empty;
        static void CheckFolder(bool force)
        {
            if (Selection.objects.Length > 0)
                return;

            string currentPath = ProjectWindowUtilBridge.GetActiveFolderPath();
            if (force || lastCheckPath != currentPath)
            {
                lastCheckPath = currentPath;
                InspectorWindowBridge.RepaintAllInspectors();
                
                if (currentPath.ToPath().TryTrimStartPath(packRootPath, out FilePath relative))
                {
                    activeFolderPath = relative;
                    UpdateDrawer(Enumerable.Repeat(relative, 1), true);
                }
                else
                {
                    activeFolderPath = FilePath.empty;
                    UpdateDrawer(Enumerable.Empty<FilePath>(), true);
                }
            }
        }

        static void UpdateDrawer(IEnumerable<FilePath> paths, bool isFolderView)
        {
            isFolderViewMode = isFolderView;
            
            if (paths.IsEmpty())
            {
                SetNewDrawer(null, Enumerable.Empty<FilePath>());
                return;
            }

            PackDrawer? drawer = drawers.FirstOrDefault(d => d.IsMatch(paths));
            SetNewDrawer(drawer, paths);
        }

        static void SetNewDrawer(PackDrawer? drawer, IEnumerable<FilePath> paths)
        {
            bool isChanged = activeDrawer != drawer;
            if (isChanged)
            {
                activeDrawer?.OnDisable();
                activeDrawer = drawer;
            }
            
            activePaths = paths.ToImmutableArray();
            activeDrawer?.OnEnable(activePaths);
            
            if (isChanged)
                onActiveDrawerChanged?.Invoke();
        }
    }
}
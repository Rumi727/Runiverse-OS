#nullable enable
using System.Collections.Immutable;
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor.Resource
{
    public static partial class PackInspectorSystem
    {
        public static readonly RuniPath packRootPath;
        
        static ImmutableArray<PackDrawer> drawers;

        public static bool isFolderViewMode { get; private set; }
        public static RuniPath activeFolderPath { get; private set; }

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
                                PackDrawer drawer = (PackDrawer)Activator.CreateInstance(x, ImmutableArray<PackDrawer.PathPair>.Empty);
                                drawer.OnEnable();

                                return drawer;
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }

                            return null;
                        })
                        .WhereNotNull()
                        .OrderByDescending(x => x.order)
                ];
            }

            Selection.selectionChanged += CheckFolder;
            EditorApplication.update += CheckFolder;
        }

        [OnCodeDeinitializing]
        static void OnCodeDeinitializing()
        {
            for (int i = 0; i < drawers.Length; i++)
                drawers[i].OnDisable();

            Selection.selectionChanged -= CheckFolder;
            EditorApplication.update -= CheckFolder;
        }

        public static PackDrawer? CreateDrawer(Object[] objects)
        {
            IEnumerable<PackDrawer.PathPair> paths = objects
                .Select(x =>
                {
                    bool success = TryGetRelativePathFrom(x, out PhysicalPath rootPath, out RuniPath path);
                    return (success, rootPath, path);
                })
                .Where(x => x.success)
                .Select(x => new PackDrawer.PathPair(x.rootPath, x.path));

            // ReSharper disable once UseCollectionExpression
            return drawers.Where(x => x.IsMatch(paths.Select(x => x.relativePath)))
                .Select(x => (PackDrawer)Activator.CreateInstance(x.GetType(), paths.ToImmutableArray()))
                .FirstOrDefault();
        }

        public static bool TryGetRelativePathFrom(Object obj, out PhysicalPath rootPath, out RuniPath path)
        {
            rootPath = (PhysicalPath)Application.streamingAssetsPath;
            path = RuniPath.From(AssetDatabase.GetAssetPath(obj));

            if (path.TryGetRelativePath(packRootPath, out path))
                return true;

            return false;
        }

        static RuniPath lastCheckPath = RuniPath.empty;
        static void CheckFolder()
        {
            if (Selection.objects.Length > 0)
            {
                isFolderViewMode = false;
                return;
            }

            RuniPath currentPath = (RuniPath)ProjectWindowUtilBridge.GetActiveFolderPath();
            if (lastCheckPath != currentPath)
            {
                lastCheckPath = currentPath;
                InspectorWindowBridge.RepaintAllInspectors();

                isFolderViewMode = true;

                if (currentPath.TryGetRelativePath(packRootPath, out RuniPath relative))
                    activeFolderPath = relative;
                else
                    activeFolderPath = RuniPath.empty;
            }
        }
    }
}
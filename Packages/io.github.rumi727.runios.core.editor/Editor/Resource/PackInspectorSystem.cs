#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections.Immutable;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Reflection;

namespace RuniOS.Editor.Resource
{
    public static partial class PackInspectorSystem
    {
        static PackInspectorSystem() => registry.onChanged += () =>
        {
            for (int i = 0; i < drawers.Length; i++)
                drawers[i].OnDisable();

            drawers = default;
        };

        public static readonly RuniPath packRootPath = PhysicalPath.From(Application.streamingAssetsPath).GetRelativePath(projectPath);

        [GenerateAssignableTypeRegistry(typeof(PackDrawer))]
        public static partial AssignableTypeRegistry registry { get; }

        public static ImmutableArray<PackDrawer> drawers
        {
            get
            {
                if (!field.IsDefault)
                    return field;

                return field =
                [
                    ..registry.registeredTypes
                        .Where(x => !x.IsAbstract && !x.IsConstructedGenericType && x.IsSubclassOf(typeof(PackDrawer)))
                        .Select(x =>
                        {
                            try
                            {
                                PackDrawer drawer = (PackDrawer)Activator.CreateInstance(x, ImmutableArray<IONode>.Empty)!;
                                // 대상이 없는 원형이라 준비가 끝나기를 기다릴 필요가 없습니다.
                                drawer.OnEnable().Forget();

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
            private set;
        } = default;

        public static PackDrawer? CreateDrawer(IEnumerable<IONode> nodes)
        {
            ImmutableArray<IONode> nodeArray = [..nodes];

            // ReSharper disable once UseCollectionExpression
            return drawers.Where(x => x.IsMatch(nodeArray.Select(x => x.path)))
                .Select(x => (PackDrawer)Activator.CreateInstance(x.GetType(), nodeArray)!)
                .FirstOrDefault();
        }
    }
}
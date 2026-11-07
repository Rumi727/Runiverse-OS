#nullable enable
using RuniOS.IO;
using RuniOS.Resource;
using RuniOS.Services;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RuniOS.Editor
{
    /// <summary>
    /// Provides the project streaming assets and the streaming assets of every registered package in the Editor.<br/>
    /// 에디터에서 프로젝트의 StreamingAssets와 등록된 모든 패키지의 StreamingAssets를 제공합니다.
    /// </summary>
    /// <remarks>
    /// The project streaming assets take priority over the package streaming assets, so the end user always has the final say over the resources.<br/>
    /// 프로젝트 StreamingAssets가 패키지 StreamingAssets보다 우선하므로, 리소스에 대한 최종 결정권은 항상 최종 사용자에게 있습니다.
    /// </remarks>
    public sealed partial class EditorStreamingAssetsService : IStreamingAssetsService
    {
        EditorStreamingAssetsService() { }

        public const string editorStreamingAssetsFolderName = "EditorStreamingAssets";

        /// <summary>
        /// Gets the Editor streaming assets service instance.<br/>
        /// 에디터 StreamingAssets 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static EditorStreamingAssetsService instance { get; } = new();

        /// <summary>
        /// The identifier of the Editor streaming assets service.<br/>
        /// 에디터 StreamingAssets 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "streaming-assets/editor");
        Identifier IService.id => id;

        /// <summary>
        /// A single streaming assets location that makes up <see cref="provider"/>.<br/>
        /// <see cref="provider"/>를 이루는 개별 StreamingAssets 위치입니다.
        /// </summary>
        /// <param name="package">
        /// The package that owns the location, or <see langword="null"/> for the project streaming assets.<br/>
        /// 위치를 가진 패키지이며, 프로젝트 StreamingAssets이면 <see langword="null"/>입니다.
        /// </param>
        /// <param name="provider">
        /// The provider of the location. The folder it refers to may not exist.<br/>
        /// Locations of packages that the Package Manager manages, such as registry or Git packages, use a read-only <see cref="PackageIOProvider"/>; the others use a writable <see cref="PhysicalIOProvider"/>.
        /// <br/><br/>
        /// 위치의 프로바이더입니다. 가리키는 폴더가 없을 수 있습니다.<br/>
        /// 레지스트리나 Git 패키지처럼 패키지 관리자가 관리하는 패키지의 위치는 읽기 전용인 <see cref="PackageIOProvider"/>를, 나머지는 쓰기 가능한 <see cref="PhysicalIOProvider"/>를 씁니다.
        /// </param>
        public readonly record struct Source(PackageInfo? package, IIOProvider provider);

        /// <summary>
        /// Gets the individual streaming assets locations in priority order, from the highest priority to the lowest.<br/>
        /// 개별 StreamingAssets 위치를 우선순위가 높은 것부터 순서대로 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The providers are the same instances that <see cref="provider"/> groups.<br/>
        /// 프로바이더는 <see cref="provider"/>가 묶는 인스턴스와 같습니다.
        /// </remarks>
        public static IReadOnlyList<Source> sources => field ??= [.. CreateSources()];

        /// <summary>
        /// Gets the individual Editor streaming assets locations in priority order, from the highest priority to the lowest.<br/>
        /// 개별 EditorStreamingAssets 위치를 우선순위가 높은 것부터 순서대로 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The providers are the same instances that <see cref="editorProvider"/> groups.<br/>
        /// 프로바이더는 <see cref="editorProvider"/>가 묶는 인스턴스와 같습니다.
        /// </remarks>
        public static IReadOnlyList<Source> editorSources => field ??= [.. CreateEditorSources()];

        /// <summary>
        /// Gets the provider that groups every location of <see cref="sources"/>.<br/>
        /// <see cref="sources"/>의 모든 위치를 묶은 프로바이더를 가져옵니다.
        /// </summary>
        public static IIOProvider provider => field ??= new GroupIOProvider(sources.Select(x => x.provider));
        IIOProvider IStreamingAssetsService.provider => provider;

        /// <summary>
        /// Gets the provider that groups every location of <see cref="editorSources"/>.<br/>
        /// <see cref="editorSources"/>의 모든 위치를 묶은 프로바이더를 가져옵니다.
        /// </summary>
        public static IIOProvider editorProvider => field ??= new GroupIOProvider(editorSources.Select(x => x.provider));

        static IEnumerable<Source> CreateSources()
        {
            yield return new Source(null, new PhysicalIOProvider((PhysicalPath)Application.streamingAssetsPath, SandboxPolicy.Unrestricted));
            foreach (var item in PackageInfo.GetAllRegisteredPackages())
                yield return new Source(item, CreatePackageProvider(item, IStreamingAssetsService.streamingAssetsFolderName));
        }

        static IEnumerable<Source> CreateEditorSources()
        {
            yield return new Source(null, new PhysicalIOProvider((PhysicalPath)Directory.GetCurrentDirectory() / "Assets" / editorStreamingAssetsFolderName, SandboxPolicy.Unrestricted));
            foreach (var item in PackageInfo.GetAllRegisteredPackages())
                yield return new Source(item, CreatePackageProvider(item, editorStreamingAssetsFolderName));
        }

        // 임베디드·로컬 패키지만 사용자가 직접 관리하는 폴더이고, 나머지는 패키지 관리자가 캐시에 내려받아 언제든 되돌리므로 읽기 전용으로 둡니다.
        static IIOProvider CreatePackageProvider(PackageInfo package, string folderName)
        {
            PhysicalPath path = (PhysicalPath)package.resolvedPath / folderName;
            if (package.source is UnityEditor.PackageManager.PackageSource.Embedded or UnityEditor.PackageManager.PackageSource.Local)
                return new PhysicalIOProvider(path, SandboxPolicy.Unrestricted);

            return new PackageIOProvider(package, path, SandboxPolicy.Unrestricted);
        }

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
    }
}

#nullable enable
using Cysharp.Threading.Tasks;
using R3;
using RuniOS.Resource;

namespace RuniOS.Editor;

/// <summary>
/// 에디터에서 리소스를 미리 로딩시키는 클래스입니다.
/// </summary>
public sealed class EditorResourceLoader : AssetPostprocessor
{
    [InitializeOnLoadMethod]
    static void Initialize() => ResourceManager.reloadStartEvent += x => x.progress.Subscribe(x => SetProgress(typeof(EditorResourceLoader).FullName ?? nameof(EditorResourceLoader), x));

    public const string progressText = "internal.editor_resource_loader.loading";

    /// <summary>
    /// <see cref="ProgressInToolbar.SetProgress"/> 메소드랑 기능적으론 동일하지만, progressText 매개변수의 값이 <see cref="progressText"/> 상수로 설정됩니다.
    /// </summary>
    /// <param name="id">한 프로그레스 바에서 여러개의 진행도를 구분할 고유 id</param>
    /// <param name="value">0에서 1 사이의 진행도</param>
    public static void SetProgress(string id, float value) => ProgressInToolbar.SetProgress(progressText, id, value);

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        if (didDomainReload || importedAssets
                .Concat(deletedAssets)
                .Concat(movedAssets)
                .Any(x => x.StartsWith("Assets/StreamingAssets", StringComparison.OrdinalIgnoreCase)))
            ResourceManager.Reload().Forget();
    }
}
#nullable enable
namespace RuniOS.Resource;

/// <summary>
/// 에셋 핸들에 대한 사용(참조)을 추적하고, 사용 완료 시 자동으로 핸들에 반환되도록 하는 래퍼 클래스입니다.
/// <br/>이 클래스는 <see cref="IDisposable"/> 패턴을 사용하여 에셋의 생명주기를 관리합니다.
/// </summary>
public abstract class AssetScope : IDisposable
{
    /// <summary>
    /// 이 스코프가 참조하는 <see cref="AssetHandle"/>을 가져옵니다.
    /// </summary>
    public AssetHandle handle { get; }

    /// <summary>
    /// <see cref="AssetScope"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="handle">이 스코프가 참조할 <see cref="AssetHandle"/>입니다.</param>
    protected AssetScope(AssetHandle handle) => this.handle = handle;
        
    /// <summary>
    /// 이 <see cref="AssetScope"/>에 대한 참조를 <see cref="handle"/>에 반환하고, 관련된 리소스를 정리합니다.
    /// </summary>
    public void Dispose()
    {
        handle.ReturnScope(this);
        GC.SuppressFinalize(this);
    }
        
    /// <summary>
    /// <see cref="AssetScope"/>가 <see cref="Dispose()"/>를 통해 명시적으로 정리되지 않고 가비지 컬렉터에 의해 정리될 때 경고를 기록합니다.
    /// </summary>
    ~AssetScope() => Debug.RuntimeLogError(
        $"AssetScope for handle '{handle.ioHandler.fullPath}' was finalized without being properly disposed.\n" +
        "This is likely a resource leak. Ensure 'Dispose()' or 'using' is used to dispose this IDisposable asset."
    );
}
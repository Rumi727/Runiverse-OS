using Microsoft.CodeAnalysis;

namespace RuniOS.CodeAnalysis.Diagnostics;

/// <summary>
/// Defines suppression descriptors used by the analyzer suppressors.<br/>
/// 분석기 억제기가 사용하는 억제 설명자를 정의합니다.
/// </summary>
static class SuppressorDiagnostics
{
    /// <summary>
    /// Describes the intentional suppression of Unity diagnostic <c>UAC1001</c> for <c>AssetRef&lt;TAsset&gt;</c>.<br/>
    /// <c>AssetRef&lt;TAsset&gt;</c>에 대한 Unity 진단 <c>UAC1001</c>을 의도적으로 억제하는 설명자입니다.
    /// </summary>
    // suppression ID ROS0001과 대상 진단 UAC1001을 AssetRefSerializationSuppressor의 필터와 맞춥니다.
    public static readonly SuppressionDescriptor descriptor = new
    (
        "ROS0001",
        "UAC1001",
        "AssetRef<TAsset> intentionally allows TAsset to be skipped by Unity serialization when unsupported."
    );
}

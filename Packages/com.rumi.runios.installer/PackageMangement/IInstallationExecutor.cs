#nullable enable
using System.Collections.Generic;
using System.Threading;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Matches installation descriptors and executes a batch of supported requirements.<br/>
    /// 설치 descriptor를 matching하고 지원하는 요구사항의 batch를 실행합니다.
    /// </summary>
    public interface IInstallationExecutor
    {
        /// <summary>
        /// Determines whether this instance supports the descriptor.<br/>
        /// 이 인스턴스가 descriptor를 지원하는지 확인합니다.
        /// </summary>
        /// <param name="installation">
        /// The descriptor to match.<br/>
        /// matching할 descriptor입니다.
        /// </param>
        /// <returns>
        /// True if supported; otherwise false.<br/>
        /// 지원하면 <see langword="true"/>, 그렇지 않으면 false를 반환합니다.
        /// </returns>
        bool CanExecute(IInstallation installation);
        /// <summary>
        /// Ensures supported requirements and produces one final result per input installation entry.<br/>
        /// 지원하는 요구사항을 만족시키고 입력 installation 항목별 최종 결과 하나를 생산합니다.
        /// </summary>
        /// <param name="installations">
        /// Descriptors accepted by this instance's CanExecute method.<br/>
        /// 이 인스턴스의 CanExecute 메서드가 수락한 descriptor들입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel execution.<br/>
        /// 실행 취소에 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// One final result per input installation entry, in implementation-defined order.<br/>
        /// 구현이 정한 순서로 입력 installation 항목별 최종 결과 하나를 반환합니다.
        /// </returns>
        IAsyncEnumerable<InstallationResult> EnsureAsync(IEnumerable<IInstallation> installations, CancellationToken cancellationToken = default);
    }
}

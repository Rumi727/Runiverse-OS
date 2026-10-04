#nullable enable
using UnityEngine;

namespace RuniOS.Editor.Installer
{
    /// <summary>
    /// Provides target-follow animation calculations for Setup windows and screens.<br/>
    /// Setup 창과 화면의 목표 추적 애니메이션 계산을 제공합니다.
    /// </summary>
    public static class SetupAnimationUtility
    {
        /// <summary>
        /// Follows the target using continuous exponential convergence over the elapsed time.<br/>
        /// 경과 시간에 대한 연속 지수 수렴으로 목표 값을 추적합니다.
        /// </summary>
        /// <param name="current">
        /// The current value.<br/>
        /// 현재 값입니다.
        /// </param>
        /// <param name="target">
        /// The target value for this time interval.<br/>
        /// 이번 시간 구간의 목표 값입니다.
        /// </param>
        /// <param name="rate">
        /// The nonnegative convergence coefficient per second.<br/>
        /// 0 이상의 초당 수렴 비율 계수입니다.
        /// </param>
        /// <param name="deltaTime">
        /// The nonnegative elapsed time in seconds.<br/>
        /// 0 이상의 경과 시간이며 단위는 초입니다.
        /// </param>
        /// <returns>
        /// The value after following the target for the elapsed time.<br/>
        /// 경과 시간 동안 목표 값을 추적한 결과입니다.
        /// </returns>
        public static float Follow(float current, float target, float rate, float deltaTime)
            => Mathf.Lerp(current, target, 1f - Mathf.Exp(-rate * deltaTime));
    }
}

#nullable enable

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Describes a current advisory observation without guaranteeing a later ensure outcome.<br/>
    /// 이후 ensure 결과를 보장하지 않고 현재 advisory 관측을 표현합니다.
    /// </summary>
    public enum InstallationPreviewStatus
    {
        /// <summary>
        /// The observed requirements are already satisfied.<br/>
        /// 관측한 요구사항이 이미 충족되어 있습니다.
        /// </summary>
        Satisfied,
        /// <summary>
        /// The current state requires ensure work.<br/>
        /// 현재 상태에 ensure 작업이 필요합니다.
        /// </summary>
        RequiresEnsure,
        /// <summary>
        /// Preparation is complete; package acquisition is delegated and is not verified.<br/>
        /// 준비가 완료되었으며 package 획득은 위임되고 검증하지 않습니다.
        /// </summary>
        Delegated,
        /// <summary>
        /// The matching executor does not provide preview capability.<br/>
        /// matching executor가 preview capability를 제공하지 않습니다.
        /// </summary>
        NotSupported,
        /// <summary>
        /// Observation failed or found an unresolved requirement.<br/>
        /// 관측에 실패했거나 해결할 수 없는 요구사항을 발견했습니다.
        /// </summary>
        Failed
    }
}

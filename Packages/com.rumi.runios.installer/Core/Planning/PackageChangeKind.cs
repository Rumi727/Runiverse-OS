#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Classifies a package-state transition independently of concrete operations.<br/>
    /// 구체적인 작업과 독립적으로 패키지 상태 전이를 분류합니다.
    /// </summary>
    public enum PackageChangeKind { Install, Update, Remove, ChangeInstallation }
}

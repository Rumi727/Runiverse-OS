#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement.Diagnostics
{
    /// <summary>
    /// Identifies the stage that emitted a diagnostic.<br/>
    /// 진단이 발생한 단계를 식별합니다.
    /// </summary>
    public enum PackageDiagnosticPhase { Registration, Resolution, Observation, Planning, Execution }
}

#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RuniOS.PackageManagement.Providers
{
    /// <summary>
    /// Returns an acquired representation without prescribing a file-system layout.<br/>
    /// 파일 시스템 배치를 지정하지 않고 획득한 표현을 반환합니다.
    /// </summary>
    public abstract class AcquiredPackageContent
    {
        /// <summary>
        /// Gets the content representation produced by acquisition.<br/>
        /// 콘텐츠 획득으로 생성한 표현을 가져옵니다.
        /// </summary>
        public abstract string representation { get; }
    }
}

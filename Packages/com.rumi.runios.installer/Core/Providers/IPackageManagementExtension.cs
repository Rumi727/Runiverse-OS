#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement;

namespace RuniOS.PackageManagement.Providers
{
    /// <summary>
    /// Registers independently implemented capabilities in a host composition root.<br/>
    /// 호스트 구성 지점에서 독립적으로 구현한 기능을 등록합니다.
    /// </summary>
    public interface IPackageManagementExtension
    {
        /// <summary>
        /// Registers only the capabilities implemented by this extension.<br/>
        /// 이 확장에서 구현한 기능만 등록합니다.
        /// </summary>
        /// <param name="builder">
        /// The mutable registration collector.<br/>
        /// 가변 등록 수집기입니다.
        /// </param>
        void Register(PackageManagementBuilder builder);
    }
}

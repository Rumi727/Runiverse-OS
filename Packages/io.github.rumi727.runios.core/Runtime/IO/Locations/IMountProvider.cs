#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Providers;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Provides the mounts of the system.<br/>
    /// 시스템의 마운트를 제공합니다.
    /// </summary>
    public interface IMountProvider : IProvider
    {
        /// <summary>
        /// Asynchronously enumerates the mounts.<br/>
        /// 마운트를 비동기로 열거합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous sequence containing the mounts.<br/>
        /// 마운트를 포함하는 비동기 시퀀스를 반환합니다.
        /// </returns>
        IUniTaskAsyncEnumerable<Mount> EnumerateMounts();
    }
}
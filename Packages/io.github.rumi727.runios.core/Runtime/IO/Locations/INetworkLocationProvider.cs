#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Providers;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Provides the network locations of the system.<br/>
    /// 시스템의 네트워크 위치를 제공합니다.
    /// </summary>
    public interface INetworkLocationProvider : IService
    {
        /// <summary>
        /// Asynchronously enumerates the network locations.<br/>
        /// 네트워크 위치를 비동기로 열거합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous sequence containing the network locations.<br/>
        /// 네트워크 위치를 포함하는 비동기 시퀀스를 반환합니다.
        /// </returns>
        IUniTaskAsyncEnumerable<NetworkLocation> EnumerateLocations();
    }
}
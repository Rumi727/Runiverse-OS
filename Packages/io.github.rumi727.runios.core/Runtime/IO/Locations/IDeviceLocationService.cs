#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Provides the device locations of the system.<br/>
    /// 시스템의 장치 위치를 제공합니다.
    /// </summary>
    public interface IDeviceLocationService : IService
    {
        /// <summary>
        /// Asynchronously enumerates the device locations.<br/>
        /// 장치 위치를 비동기로 열거합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous sequence containing the device locations.<br/>
        /// 장치 위치를 포함하는 비동기 시퀀스를 반환합니다.
        /// </returns>
        IUniTaskAsyncEnumerable<DeviceLocation> EnumerateLocations();
    }
}
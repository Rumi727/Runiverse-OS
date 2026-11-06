#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Providers;

namespace RuniOS.IO.Locations
{
    public interface IDeviceLocationProvider : IProvider
    {
        IUniTaskAsyncEnumerable<DeviceLocation> EnumerateLocations();
    }
}
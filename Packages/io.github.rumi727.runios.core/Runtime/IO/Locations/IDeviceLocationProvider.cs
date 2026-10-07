#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.IO.Locations
{
    public interface IDeviceLocationProvider
    {
        public static IDeviceLocationProvider? instance { get; private set; } = null;

        IUniTaskAsyncEnumerable<DeviceLocation> EnumerateLocations();

        public static void Register(IDeviceLocationProvider provider)
        {
            if (instance != null)
                throw new InvalidOperationException("The IDeviceLocationProvider instance must be globally unique!");

            instance = provider;
        }

        public static void Unregister(IDeviceLocationProvider provider)
        {
            if (instance != provider)
                throw new InvalidOperationException("This is an unregistered IDeviceLocationProvider instance!");

            instance = null;
        }
    }
}
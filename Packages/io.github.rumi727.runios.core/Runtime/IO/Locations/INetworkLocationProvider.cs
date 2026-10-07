#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.IO.Locations
{
    public interface INetworkLocationProvider
    {
        public static INetworkLocationProvider? instance { get; private set; } = null;

        IUniTaskAsyncEnumerable<NetworkLocation> EnumerateLocations();

        public static void Register(INetworkLocationProvider provider)
        {
            if (instance != null)
                throw new InvalidOperationException("The INetworkLocationProvider instance must be globally unique!");

            instance = provider;
        }

        public static void Unregister(INetworkLocationProvider provider)
        {
            if (instance != provider)
                throw new InvalidOperationException("This is an unregistered INetworkLocationProvider instance!");

            instance = null;
        }
    }
}
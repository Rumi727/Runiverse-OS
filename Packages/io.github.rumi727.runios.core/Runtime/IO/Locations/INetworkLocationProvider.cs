#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Providers;

namespace RuniOS.IO.Locations
{
    public interface INetworkLocationProvider : IProvider
    {
        IUniTaskAsyncEnumerable<NetworkLocation> EnumerateLocations();
    }
}
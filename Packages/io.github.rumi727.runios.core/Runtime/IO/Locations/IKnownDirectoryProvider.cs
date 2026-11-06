#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Providers;

namespace RuniOS.IO.Locations
{
    public interface IKnownDirectoryProvider : IProvider
    {
        IUniTaskAsyncEnumerable<KnownDirectory> EnumerateDirectories();

        async UniTask<KnownDirectory?> TryGetDirectory(KnownDirectoryType type)
        {
            await foreach (var item in EnumerateDirectories())
            {
                if (type != item.type)
                    continue;

                return item;
            }

            return null;
        }
    }
}
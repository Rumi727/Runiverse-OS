#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.IO.Locations
{
    public interface IKnownDirectoryProvider
    {
        public static IKnownDirectoryProvider? instance { get; private set; } = null;

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

        public static void Register(IKnownDirectoryProvider provider)
        {
            if (instance != null)
                throw new InvalidOperationException("The IKnownDirectoryProvider instance must be globally unique!");

            instance = provider;
        }

        public static void Unregister(IKnownDirectoryProvider provider)
        {
            if (instance != provider)
                throw new InvalidOperationException("This is an unregistered IKnownDirectoryProvider instance!");

            instance = null;
        }
    }
}
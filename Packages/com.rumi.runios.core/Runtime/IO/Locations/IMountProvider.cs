#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.IO.Locations
{
    public interface IMountProvider
    {
        public static IMountProvider? instance { get; private set; } = null;

        IUniTaskAsyncEnumerable<Mount> EnumerateMounts();

        public static void Register(IMountProvider provider)
        {
            if (instance != null)
                throw new InvalidOperationException("The IMountProvider instance must be globally unique!");

            instance = provider;
        }

        public static void Unregister(IMountProvider provider)
        {
            if (instance != provider)
                throw new InvalidOperationException("This is an unregistered IMountProvider instance!");

            instance = null;
        }
    }
}
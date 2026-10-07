#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Linq;

namespace RuniOS.Milestones
{
    public static partial class MilestoneDispatcher
    {
        static readonly Dictionary<Type, Action> _methods = [];
        internal static IReadOnlyDictionary<Type, Action> methods { get; } = _methods.AsReadOnly();

        static readonly Dictionary<Type, Func<UniTask>> _asyncMethods = [];
        internal static IReadOnlyDictionary<Type, Func<UniTask>> asyncMethods { get; } = _asyncMethods.AsReadOnly();

        public static void Register<T>(Action method) where T : MilestoneAttribute => Register(typeof(T), method);

        public static void Register<T>(Func<UniTask> method) where T : MilestoneAttribute => Register(typeof(T), method);

        public static void Register(Type type, Action method)
        {
            if (!typeof(MilestoneAttribute).IsAssignableFrom(type))
                throw new ArgumentException();

            Action? value = _methods.GetValueOrDefault(type);
            _methods[type] = value + method;
        }

        public static void Register(Type type, Func<UniTask> method)
        {
            if (!typeof(MilestoneAttribute).IsAssignableFrom(type))
                throw new ArgumentException();

            Func<UniTask>? value = _asyncMethods.GetValueOrDefault(type);
            _asyncMethods[type] = value + method;
        }

        internal static UniTask Invoke<T>() where T : MilestoneAttribute => Invoke(typeof(T));

        internal static async UniTask Invoke(Type type)
        {
            if (!typeof(MilestoneAttribute).IsAssignableFrom(type))
                throw new ArgumentException();

            methods.GetValueOrDefault(type)?.SafeInvoke();

            if (!MilestoneDispatcher.asyncMethods.TryGetValue(type, out Func<UniTask>? asyncMethod))
                return;

            Func<UniTask>[] asyncMethods =
            [
                ..asyncMethod
                    .GetInvocationList()
                    .Cast<Func<UniTask>>()
            ];

            await UniTask.WhenAll(asyncMethods.Select(InvokeAsync));
        }

        static async UniTask InvokeAsync(Func<UniTask> method)
        {
            try
            {
                await method.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
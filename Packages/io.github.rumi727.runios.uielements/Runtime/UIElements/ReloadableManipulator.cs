#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public class ReloadableManipulator : Manipulator, IReloadable
    {
        public ReloadableManipulator() { }
        public ReloadableManipulator(Action onReload) => this.onReload = onReload;
        public ReloadableManipulator(Func<UniTask> onReloadAsync) => this.onReloadAsync = onReloadAsync;

        public event Action? onReload;
        public event Func<UniTask>? onReloadAsync;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<AttachToPanelEvent>(OnAttach);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);

            ResourceManager.DetachReloadable(this);
        }

        protected virtual void OnAttach(AttachToPanelEvent evt)
        {
            ResourceManager.AttachReloadable(this);
            Reload().Forget();
        }

        protected virtual void OnDetach(DetachFromPanelEvent evt) => ResourceManager.DetachReloadable(this);

        public virtual UniTask Reload()
        {
            onReload?.Invoke();
            return onReloadAsync?.Invoke() ?? UniTask.CompletedTask;
        }
    }
}
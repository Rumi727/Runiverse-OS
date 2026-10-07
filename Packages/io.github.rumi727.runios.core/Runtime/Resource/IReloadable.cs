using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public interface IReloadable
    {
        public UniTask Reload();
    }
}
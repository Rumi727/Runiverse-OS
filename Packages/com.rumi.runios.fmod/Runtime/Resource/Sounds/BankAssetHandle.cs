#nullable enable
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using RuniOS.IO;

namespace RuniOS.Resource.Sounds
{
    public class BankAssetHandle : AssetHandle<Bank>
    {
        public BankAssetHandle(IONode node, IOMetaData metaData) : base(node, metaData) { }
        
        protected override async UniTask<Bank> Load()
        {
            byte[] datas = await node.file.ReadAllBytes();
            RuntimeManager.StudioSystem.loadBankMemory(datas, LOAD_BANK_FLAGS.NORMAL | LOAD_BANK_FLAGS.NONBLOCKING, out Bank bank).ThrowIfNotOk();

            await UniTask.WaitWhile(() =>
            {
                bank.getLoadingState(out LOADING_STATE state).ThrowIfNotOk();
                return state == LOADING_STATE.LOADING;
            });
            
            return bank;
        }
        
        protected override void Unload() => assetObject.unload().ThrowIfNotOk();

        protected override bool IsDefaultAsset(Bank asset) => !asset.hasHandle();
        protected override Bank GetDefaultAsset() => new Bank();
        
        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other))
                return false;

            return assetObject.isValid();
        }
    }
}
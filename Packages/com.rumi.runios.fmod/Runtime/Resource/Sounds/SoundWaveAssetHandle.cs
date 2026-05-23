#nullable enable
using Cysharp.Threading.Tasks;
using FMOD;
using RuniOS.Sounds;
using RuniOS.IO;
using System.Runtime.InteropServices;

namespace RuniOS.Resource.Sounds
{
    public class SoundWaveAssetHandle(IONode node, FileMetaData metaData) : AssetHandle<Sound>(node, metaData)
    {
        protected override async UniTask<Sound> Load()
        {
            byte[] datas = await node.file.ReadAllBytes();
            
            CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO
            {
                cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
                length = (uint)datas.Length
            };
            
            // 1. NONBLOCKING으로 사운드 생성 시작
            SoundWaveManager.currentSystem.createSound(datas, MODE.OPENMEMORY | MODE.NONBLOCKING, ref exinfo, out Sound sound).ThrowIfNotOk();

            try
            {
                // 2. 로딩 완료 대기
                await UniTask.WaitWhile(() =>
                {
                    sound.getOpenState(out OPENSTATE openstate, out _, out _, out _).ThrowIfNotOk();
                    return openstate == OPENSTATE.LOADING;
                });
            
                // 3. 루프 종료 후 최종 상태 확인
                sound.getOpenState(out OPENSTATE finalOpenstate, out _, out _, out _).ThrowIfNotOk();

                if (finalOpenstate != OPENSTATE.READY)
                {
                    // 4. 로딩 실패/취소 시 자원 정리
                    // assetObject에 할당되기 전에 스스로 해제합니다.
                    sound.release().ThrowIfNotOk(); 
                
                    if (finalOpenstate == OPENSTATE.ERROR)
                        throw new InvalidOperationException("FMOD Sound asynchronous loading failed with OPENSTATE.ERROR.");
                
                    throw new InvalidOperationException($"FMOD Sound finished loading in unexpected state: {finalOpenstate}.");
                }
            
                return sound;
            }
            catch
            {
                sound.release().ThrowIfNotOk();
                throw;
            }
        }
        
        protected override void Unload() => assetObject.release().ThrowIfNotOk();

        protected override bool IsDefaultAsset(Sound asset) => !asset.hasHandle();
        
        protected override Sound GetDefaultAsset() => new Sound();

        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other))
                return false;
            
            if (!assetObject.hasHandle() || assetObject.getOpenState(out OPENSTATE openstate, out _, out _, out _) != RESULT.OK)
                return false;

            return openstate != OPENSTATE.ERROR;
        }
    }
}
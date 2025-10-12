#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using UnityEngine;
using UnityEngine.LowLevel;

namespace RuniOS.Booting
{
    /// <summary>
    /// ROS의 부팅을 담당하는 클래스입니다. 초기 로딩을 수행합니다.
    /// </summary>
    public static class BootLoader
    {
        // UniTask 버그로 인해 작업이 취소되는 문제가 있음
#if FALSE //UNITY_2020_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        static async UniTaskVoid Boot()
        {
            Debug.RuntimeLog("UniTask Initialize", nameof(BootLoader));
            
            //UniTask Setting
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref loop);

            //Awaken Invoke
            Debug.RuntimeLog("Awaken Method Invoke", nameof(BootLoader));
            await ReflectionUtility.InvokeDefinedMethods<AwakenAttribute>();
            
            Debug.RuntimeLog("Loading the resource registry", nameof(BootLoader));
            
            await ResourceManager.Reload();
            
            //Starten Invoke
            Debug.RuntimeLog("Starten Method Invoke", nameof(BootLoader));
            await ReflectionUtility.InvokeDefinedMethods<StartenAttribute>();
            
            Debug.RuntimeLog("Exit bootloader", nameof(BootLoader));
        }
    }
}

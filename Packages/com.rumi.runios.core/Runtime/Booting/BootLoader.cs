#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;

namespace RuniOS.Booting
{
    /// <summary>
    /// ROS의 부팅을 담당하는 클래스입니다. 초기 로딩을 수행합니다.
    /// </summary>
    public static class BootLoader
    {
#if UNITY_2020_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        static async UniTaskVoid Boot()
        {
            Debug.Log("BootLoder");
            
            //UniTask Setting
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref loop);
            PlayerLoop.SetPlayerLoop(loop);

            //Awaken Invoke
            ReflectionUtility.AttributeInvoke<AwakenAttribute>();
            
            await UniTask.CompletedTask;
        }
    }
}

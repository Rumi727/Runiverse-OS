#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;

namespace RuniEngine.Booting
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
            //UniTask Setting
            PlayerLoopSystem loopSystems = PlayerLoop.GetDefaultPlayerLoop();
            PlayerLoopHelper.Initialize(ref loopSystems);

            //Awaken Invoke
            ReflectionUtility.AttributeInvoke<AwakenAttribute>();

            //Custom Loop Setting
            CustomPlayerLoopSetter.EventRegister(ref loopSystems);

            await UniTask.CompletedTask;
        }
    }
}

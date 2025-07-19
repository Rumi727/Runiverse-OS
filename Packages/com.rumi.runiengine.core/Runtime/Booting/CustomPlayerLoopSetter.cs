#nullable enable
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RuniEngine.Booting
{
    /// <summary>
    /// 이 클래스에 있는 모든 이벤트는 플레이 모드가 해제될 때 자동으로 이벤트가 해제됩니다
    /// <br/><br/>
    /// 엔진이 초기화 되기 전에 등록해야합니다!
    /// <br/>
    /// Awaken 어트리뷰트를 사용하는걸 추천합니다
    /// </summary>
    public static class CustomPlayerLoopSetter
    {
        public static event PlayerLoopSystem.UpdateFunction? initEvent;
        public static event PlayerLoopSystem.UpdateFunction? earlyUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? fixedUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? preUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? updateEvent;
        public static event PlayerLoopSystem.UpdateFunction? preLateUpdateEvent;
        public static event PlayerLoopSystem.UpdateFunction? postLateUpdateEvent;
#if UNITY_2020_2_OR_NEWER
        public static event PlayerLoopSystem.UpdateFunction? timeUpdateEvent;
#endif

        [Awaken]
        static void Awaken()
        {
            Kernel.quitting += () =>
            {
                PlayerLoopSystem loopSystems = PlayerLoop.GetDefaultPlayerLoop();
                PlayerLoopHelper.Initialize(ref loopSystems);
                PlayerLoop.SetPlayerLoop(loopSystems);

                initEvent = null;
                earlyUpdateEvent = null;
                fixedUpdateEvent = null;
                preLateUpdateEvent = null;
                postLateUpdateEvent = null;
                updateEvent = null;
                preLateUpdateEvent = null;
                postLateUpdateEvent = null;
#if UNITY_2020_2_OR_NEWER
                timeUpdateEvent = null;
#endif
            };
        }

        public static void EventRegister(ref PlayerLoopSystem loopSystems)
        {
            for (int i = 0; i < loopSystems.subSystemList.Length; i++)
            {
                PlayerLoopSystem loopSystem = loopSystems.subSystemList[i];
                PlayerLoopSystem.UpdateFunction? updateDelegate = null;
                Type type = loopSystem.type;

                if (type == typeof(Initialization))
                    updateDelegate += initEvent;
                else if (type == typeof(EarlyUpdate))
                    updateDelegate += earlyUpdateEvent;
                else if (type == typeof(FixedUpdate))
                    updateDelegate += fixedUpdateEvent;
                else if (type == typeof(PreUpdate))
                    updateDelegate += preUpdateEvent;
                else if (type == typeof(Update))
                    updateDelegate += updateEvent;
                else if (type == typeof(PreLateUpdate))
                    updateDelegate += preLateUpdateEvent;
                else if (type == typeof(PostLateUpdate))
                    updateDelegate += postLateUpdateEvent;
#if UNITY_2020_2_OR_NEWER
                else if (type == typeof(TimeUpdate))
                    updateDelegate += timeUpdateEvent;
#endif

                loopSystem.updateDelegate += updateDelegate;
                loopSystems.subSystemList[i] = loopSystem;
            }

            PlayerLoop.SetPlayerLoop(loopSystems);
        }
    }
}

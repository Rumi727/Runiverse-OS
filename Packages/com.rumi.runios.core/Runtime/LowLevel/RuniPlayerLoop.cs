#nullable enable
using RuniOS.Booting;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RuniOS.LowLevel
{
    /// <summary>
    /// 유니티의 PlayerLoop 시스템에 델리게이트를 등록하거나 제거하는 정적 클래스입니다.
    /// <br/>이 클래스를 통해 등록된 모든 이벤트는 플레이 모드가 종료될 때 자동으로 해제됩니다.
    /// <br/>코드 진입점으로 <see cref="AwakenAttribute"/> 사용을 권장합니다.
    /// </summary>
    public static class RuniPlayerLoop
    {
        /// <summary>
        /// 초기화(Initialization) 단계에서 실행되는 이벤트입니다.
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onInit
        {
            add => Register<Initialization>(value);
            remove => Unregister<Initialization>(value);
        }
        /// <summary>
        /// EarlyUpdate 단계에서 실행되는 이벤트입니다.
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onEarlyUpdate
        {
            add => Register<EarlyUpdate>(value);
            remove => Unregister<EarlyUpdate>(value);
        }
        /// <summary>
        /// FixedUpdate 단계에서 실행되는 이벤트입니다.
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onFixedUpdate
        {
            add => Register<FixedUpdate>(value);
            remove => Unregister<FixedUpdate>(value);
        }
        /// <summary>
        /// PreUpdate 단계에서 실행되는 이벤트입니다.
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onPreUpdate
        {
            add => Register<PreUpdate>(value);
            remove => Unregister<PreUpdate>(value);
        }
        /// <summary>
        /// Update 단계에서 실행되는 이벤트입니다.
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onUpdate
        {
            add => Register<Update>(value);
            remove => Unregister<Update>(value);
        }
        /// <summary>
        /// PreLateUpdate 단계에서 실행되는 이벤트입니다.
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onPreLateUpdate
        {
            add => Register<PreLateUpdate>(value);
            remove => Unregister<PreLateUpdate>(value);
        }
        /// <summary>
        /// PostLateUpdate 단계에서 실행되는 이벤트입니다.
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onPostLateUpdate
        {
            add => Register<PostLateUpdate>(value);
            remove => Unregister<PostLateUpdate>(value);
        }
#if UNITY_2020_2_OR_NEWER
        /// <summary>
        /// TimeUpdate 단계에서 실행되는 이벤트입니다. (Unity 2020.2 이상)
        /// </summary>
        public static event PlayerLoopSystem.UpdateFunction? onTimeUpdate
        {
            add => Register<TimeUpdate>(value);
            remove => Unregister<TimeUpdate>(value);
        }
#endif

        /// <summary>
        /// 지정된 타입의 PlayerLoop 시스템에 델리게이트를 등록합니다.
        /// </summary>
        /// <typeparam name="T">델리게이트를 등록할 PlayerLoop 시스템의 타입입니다.</typeparam>
        /// <param name="updateDelegate">등록할 델리게이트입니다.</param>
        /// <returns>
        /// 델리게이트 등록에 성공했으면 <see langword="true"/>를,
        /// 일치하는 타입을 찾지 못했거나 <paramref name="updateDelegate"/>가 <see langword="null"/>이어서 실패했으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool Register<T>(PlayerLoopSystem.UpdateFunction? updateDelegate) => Register(typeof(T), updateDelegate);
        
        /// <summary>
        /// 지정된 타입의 PlayerLoop 시스템에 델리게이트를 등록합니다.
        /// </summary>
        /// <param name="targetType">델리게이트를 등록할 PlayerLoop 시스템의 타입입니다.</param>
        /// <param name="updateDelegate">등록할 델리게이트입니다.</param>
        /// <returns>
        /// 델리게이트 등록에 성공했으면 <see langword="true"/>를,
        /// 일치하는 타입을 찾지 못했거나 <paramref name="targetType"/>이 <see langword="null"/> 또는 <paramref name="updateDelegate"/>가 <see langword="null"/>이어서 실패했으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool Register(Type? targetType, PlayerLoopSystem.UpdateFunction? updateDelegate)
        {
            if (!Kernel.isPlaying)
                throw new InvalidOperationException();

            if (targetType == null || updateDelegate == null)
                return false;

            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            bool result = InternalRegister(ref loop, targetType, updateDelegate);
            PlayerLoop.SetPlayerLoop(loop);

            return result;
        }
        
        /// <summary>
        /// PlayerLoop 시스템 구조를 재귀적으로 탐색하여 특정 델리게이트를 등록합니다.
        /// </summary>
        /// <param name="loop">탐색을 시작할 PlayerLoop 시스템입니다.</param>
        /// <param name="targetType">델리게이트를 등록할 PlayerLoop 시스템의 타입입니다.</param>
        /// <param name="updateDelegate">등록할 델리게이트입니다.</param>
        /// <returns>
        /// 델리게이트 등록에 성공했으면 <see langword="true"/>를,
        /// 일치하는 타입을 찾지 못했거나 <paramref name="targetType"/>이 <see langword="null"/> 또는 <paramref name="updateDelegate"/>가 <see langword="null"/>이어서 실패했으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        static bool InternalRegister(ref PlayerLoopSystem loop, Type? targetType, PlayerLoopSystem.UpdateFunction? updateDelegate)
        {
            if (targetType == null || updateDelegate == null || loop.subSystemList == null)
                return false;
            
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                PlayerLoopSystem item = loop.subSystemList[i];
                if (item.type == targetType)
                {
                    item.updateDelegate += updateDelegate;
                    loop.subSystemList[i] = item;
                    
                    return true;
                }

                if (InternalRegister(ref item, targetType, updateDelegate))
                {
                    loop.subSystemList[i] = item;
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 지정된 타입의 PlayerLoop 시스템에서 델리게이트를 제거합니다.
        /// </summary>
        /// <typeparam name="T">델리게이트를 제거할 PlayerLoop 시스템의 타입입니다.</typeparam>
        /// <param name="updateDelegate">제거할 델리게이트입니다.</param>
        public static void Unregister<T>(PlayerLoopSystem.UpdateFunction? updateDelegate) => Unregister(typeof(T), updateDelegate);
        
        /// <summary>
        /// 특정 타입의 PlayerLoopSystem에서 지정된 델리게이트 체인에 속한 모든 메소드를 제거합니다.
        /// </summary>
        /// <param name="targetType">제거할 PlayerLoopSystem의 타입입니다.</param>
        /// <param name="updateDelegate">
        /// 제거할 메소드들을 포함하는 델리게이트입니다. 
        /// <br/>이 델리게이트에 연결된 모든 메소드들이 제거됩니다.
        /// </param>
        public static void Unregister(Type targetType, PlayerLoopSystem.UpdateFunction? updateDelegate)
        {
            if (!Kernel.isPlaying)
                throw new InvalidOperationException();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (updateDelegate == null || targetType == null)
                return;

            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            InternalUnregister(ref loop, targetType, updateDelegate);
            PlayerLoop.SetPlayerLoop(loop);
        }

        /// <summary>
        /// 모든 PlayerLoop 시스템에서 지정된 델리게이트를 제거합니다.
        /// </summary>
        /// <param name="updateDelegate">제거할 델리게이트입니다. 이 델리게이트에 연결된 모든 메소드들이 제거됩니다.</param>
        public static void UnregisterAll(PlayerLoopSystem.UpdateFunction? updateDelegate)
        {
            if (!Kernel.isPlaying)
                throw new InvalidOperationException();

            if (updateDelegate == null)
                return;
            
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            InternalUnregister(ref loop, null, updateDelegate);
            PlayerLoop.SetPlayerLoop(loop);
        }

        /// <summary>
        /// PlayerLoop 시스템 구조를 재귀적으로 탐색하여 특정 델리게이트를 제거합니다.
        /// <br/>이 메소드는 내부용으로, <paramref name="targetType"/>이 <see langword="null"/>일 경우 모든 PlayerLoop 시스템에서 델리게이트를 제거합니다.
        /// </summary>
        /// <param name="loop">탐색을 시작할 PlayerLoop 시스템입니다.</param>
        /// <param name="targetType">제거할 PlayerLoopSystem의 타입입니다. <see langword="null"/>일 경우 모든 시스템에서 제거합니다.</param>
        /// <param name="updateDelegate">제거할 메소드들을 포함하는 델리게이트입니다.</param>
        static void InternalUnregister(ref PlayerLoopSystem loop, Type? targetType, PlayerLoopSystem.UpdateFunction? updateDelegate)
        {
            if (updateDelegate == null || loop.subSystemList == null)
                return;
            
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                PlayerLoopSystem item = loop.subSystemList[i];
                if (targetType == null || item.type == targetType)
                {
                    Delegate[] delegatesToRemove = updateDelegate.GetInvocationList();
                    foreach (var del in delegatesToRemove)
                        item.updateDelegate -= (PlayerLoopSystem.UpdateFunction)del;
                }

                InternalUnregister(ref item, targetType, updateDelegate);
                loop.subSystemList[i] = item;
            }
        }
    }
}
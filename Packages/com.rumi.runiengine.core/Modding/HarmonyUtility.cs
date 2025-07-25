#nullable enable
using Cysharp.Threading.Tasks;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace RuniEngine.Modding
{
    public static class HarmonyUtility
    {
        /// <summary>
        /// 지정된 어셈블리에 Harmony 패치를 적용하며, Unity 에디터에 특화된 동작을 수행합니다.
        /// </summary>
        /// <param name="harmony">패치에 사용할 Harmony 인스턴스입니다.</param>
        /// <param name="assembly">패치를 적용할 어셈블리입니다. 지정하지 않으면 이 메서드를 호출하는 어셈블리가 사용됩니다.</param>
        /// <remarks>
        /// <para>Unity 에디터에서 실행될 경우, 이 메서드는 <see cref="Harmony.PatchAll(Assembly)"/> 호출을 몇 프레임 지연시킵니다.
        /// 이 지연은 에디터 시작 또는 특정 에디터 이벤트 중 즉각적인 패치로 인해 발생할 수 있는 충돌이나 문제를 방지하는 데 도움이 될 수 있습니다.</para>
        /// <para>런타임(빌드된 게임) 중에는 동일한 에디터 관련 고려 사항이 적용되지 않으므로 <see cref="Harmony.PatchAll(Assembly)"/>가 지연 없이 즉시 호출됩니다.</para>
        /// <para>어셈블리를 명시적으로 지정하지 않으면, 이 메서드를 호출하는 어셈블리가 자동으로 선택되어 패치가 적용됩니다.</para>
        /// </remarks>
        public static void EditorPatchAll(Harmony harmony, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
#if UNITY_EDITOR
            if (Application.isPlaying)
                harmony.PatchAll(assembly);
            else
                InternalEditorPatchAll(harmony, assembly).Forget();
#else
            harmony.PatchAll(assembly);
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 
        /// </summary>
        /// <param name="harmony"></param>
        /// <param name="assembly"></param>
        static async UniTaskVoid InternalEditorPatchAll(Harmony harmony, Assembly assembly)
        {
            await UniTask.NextFrame(PlayerLoopTiming.Initialization);
            harmony.PatchAll(assembly);
        }
#endif
    }
}
#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Linq;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace RuniOS.Reflection
{
    public static class ReflectionUtility
    {
        static ReflectionUtility() => Refresh();

        /// <summary>
        /// 현재 로드된 모든 어셈블리 목록입니다.<br/>
        /// 이 목록은 <see cref="Refresh"/> 메서드에 의해 업데이트됩니다.
        /// <br/><br/>
        /// 이 속성은 스레드에 안전합니다.
        /// </summary>
        public static ImmutableArray<Assembly> assemblies { get; private set; } = ImmutableArray<Assembly>.Empty;
        static readonly object assembliesLock = new();

        /// <summary>
        /// 현재 로드된 모든 형식(<see cref="Type"/>) 목록입니다.<br/>
        /// 이 목록은 <see cref="Refresh"/> 메서드에 의해 업데이트됩니다.
        /// <br/><br/>
        /// 이 속성은 스레드에 안전합니다.
        /// </summary>
        public static ImmutableArray<Type> types { get; private set; } = ImmutableArray<Type>.Empty;



        /// <summary>
        /// <see cref="assemblies"/> 또는 <see cref="types"/> 목록이 <see cref="Refresh"/> 메서드를 통해
        /// 업데이트되었을 때 발생합니다.<br/>
        /// 이 이벤트 핸들러 추가/제거 및 호출은 내부적으로 잠금(<see langword="lock"/>)을 사용하여
        /// 스레드에 안전하게 보호됩니다.<br/>
        /// <b>경고:</b> 이 이벤트의 핸들러 내에서 <see cref="onListUpdate"/>에 접근하거나 호출하는 것은
        /// <b>데드락(Deadlock)</b>을 유발할 수 있으므로, <b>절대</b> 사용하지 않아야 합니다.
        /// </summary>
        public static event Action? onListUpdate
        {
            add
            {
                lock (onListUpdateLock)
                    _onListUpdate += value;
            }
            remove
            {
                lock (onListUpdateLock)
                    _onListUpdate -= value;
            }
        }
        static Action? _onListUpdate;
        static readonly object onListUpdateLock = new();



        public static bool IsAsyncMethod(this MethodBase methodBase) => methodBase.IsDefined(typeof(AsyncStateMachineAttribute));

        public static bool IsCompilerGenerated(this Type type) => type.IsDefined(typeof(CompilerGeneratedAttribute));
        public static bool IsCompilerGenerated(this MemberInfo memberInfo) => memberInfo.IsDefined(typeof(CompilerGeneratedAttribute));



        /// <summary>
        /// 현재 애플리케이션 도메인에 로드된 어셈블리와 형식 목록을 새로고침(업데이트)합니다.<br/>
        /// 이 메서드는 내부적으로 잠금(<see langword="lock"/>)을 사용하여 스레드에 안전하게 데이터를 갱신합니다.
        /// </summary>
        public static void Refresh()
        {
            lock (assembliesLock)
            {
                try
                {
#if UNITY_6000_6_OR_NEWER
                    assemblies = [..UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()];
#else
                    assemblys = AppDomain.CurrentDomain.GetAssemblies().ToImmutableArray();
#endif
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                types = [
                    ..assemblies
#if UNITY_EDITOR
                        .Where(x => 
                            /* 브릿지 코드 제외 */ !x.FullName.StartsWith("RuniOS.Editor.APIBridge", StringComparison.Ordinal) &&
                            /* 병신; */ !x.FullName.StartsWith(nameof(JetBrains), StringComparison.Ordinal))
#endif
                        .SelectMany(static x =>
                        {
                            try
                            {
                                return x.GetTypes();
                            }
                            catch (ReflectionTypeLoadException e)
                            {
                                Debug.LogException(e);
                                return e.Types.Where(static x => x != null);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }

                            return [];
                        })
                ];
            }
            
            lock (onListUpdateLock)
                _onListUpdate.SafeInvoke();
        }

        /// <summary>
        /// <typeparamref name="T"/> 어트리뷰트가 정의된 모든 정적이며 매개변수가 없는(parameterless)
        /// 메소드들을 리플렉션을 사용하여 비동기적으로 순회하며 호출합니다.<br/>
        /// <b>메소드의 매개변수 개수 체크</b>와 경고 메시지 출력은
        /// <b>개발 빌드나 에디터 환경</b>에서만 이루어집니다.<br/>
        /// 메소드 탐색은 백그라운드 스레드풀에서 수행됩니다.
        /// </summary>
        /// <typeparam name="T">찾을 메소드에 정의된 <see cref="PreserveAttribute"/> 타입입니다.</typeparam>
        public static async UniTask InvokeDefinedMethods<T>() where T : PreserveAttribute
        {
            // Linq 쓰면 코드가 몇배는 깔끔해지겠지만, 메소드 호출이 너무 길어져 대략 2배에서 심하면 10배까지도 성능적인 차이가 나는것을 확인했습니다.
            // 소스 제너레이터를 사용해도 되지만, 모딩 환경을 고려하여 리플렉션으로 결정했습니다.
            List<MethodInfo> methods = [];

#if UNITY_EDITOR || ENABLE_PROFILER
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            await UniTask.RunOnThreadPool(() =>
            {
                for (int i = 0; i < types.Length; i++)
                {
                    foreach (var method in types[i].GetRuntimeMethods())
                    {
                        if (!method.IsStatic || method.IsSpecialName || method.IsAbstract)
                            return;

                        if (!method.IsDefined(typeof(T)))
                            continue;
                        
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
                        if (!method.GetParameters().IsEmpty())
                        {
                            Debug.RuntimeLogWarning
                            (
                                $"A method {method.DeclaringType?.Name}.{method.Name} defined as an attribute has been found with a non-zero number of parameters.\n" +
                                "This is currently ignored, but the built program will not check the number of parameters, so this will cause problems!"
                            );
                            return;
                        }
#endif
                        methods.Add(method);
                    }
                }
            });

#if UNITY_EDITOR || ENABLE_PROFILER
            Debug.RuntimeLog($"It took {stopwatch.Elapsed.TotalSeconds} seconds to create a list of methods that match the condition.", $"{nameof(ReflectionUtility)}.{nameof(InvokeDefinedMethods)}<{typeof(T).Name}>");
#endif

            foreach (var item in methods)
            {
                try
                {
                    item.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                
                await UniTask.Yield();
            }
        }
        
        public static MethodInfo GetMethodInfo(Delegate method) => method.Method;
        
        /// <summary>
        /// 문자열과 배열은 포함되지 않습니다!
        /// </summary>
        public static bool HasDefaultConstructor(this Type t, bool includeNonPublic = false)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
                flags |= BindingFlags.NonPublic; 
            
            return t.IsValueType || t.GetConstructor(flags, null, Type.EmptyTypes, null) != null;
        }
        
        public static bool IsFlags(this Enum value) => value.GetType().IsDefined(typeof(FlagsAttribute));

        [return: NotNullIfNotNull("value")]
        public static object? Cast(this Type Type, object? value)
        {
            if (value == null)
                return null;

            ParameterExpression param = Expression.Parameter(typeof(object), nameof(value));
            BlockExpression block = Expression.Block(Expression.Convert(Expression.Convert(param, value.GetType()), Type));
            Delegate run = Expression.Lambda(block, param).Compile();
            
            return run.DynamicInvoke(value);
        }
    }
}
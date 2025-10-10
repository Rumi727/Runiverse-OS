#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace RuniOS
{
    public static class ReflectionUtility
    {
        static ReflectionUtility() => Refresh();

        /// <summary>
        /// All loaded assemblys
        /// </summary>
        public static IReadOnlyList<Assembly> assemblys { get; private set; } = Array.Empty<Assembly>();

        /// <summary>
        /// All loaded types
        /// </summary>
        public static IReadOnlyList<Type> types { get; private set; } = Array.Empty<Type>();



        public static bool IsAsyncMethod(this MethodBase methodBase) => methodBase.IsDefined(typeof(AsyncStateMachineAttribute));

        public static bool IsCompilerGenerated(this Type type) => type.IsDefined(typeof(CompilerGeneratedAttribute));
        public static bool IsCompilerGenerated(this MemberInfo memberInfo) => memberInfo.IsDefined(typeof(CompilerGeneratedAttribute));



        public static void Refresh()
        {
            try
            {
                assemblys = Array.AsReadOnly(AppDomain.CurrentDomain.GetAssemblies());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            types = assemblys.Where(x => /* 병신; */ !x.FullName.StartsWith("JetBrains", StringComparison.Ordinal))
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
                    
                return Array.Empty<Type>();
            }).ToArray().AsReadOnly();
        }

        /// <summary>
        /// <typeparamref name="T"/> 어트리뷰트가 정의된 모든 정적이며 매개변수가 없는(parameterless)
        /// 메소드들을 리플렉션을 사용하여 비동기적으로 순회하며 호출합니다.<br/>
        /// <b>메소드의 매개변수 개수 체크</b>와 경고 메시지 출력은
        /// <b>개발 빌드나 에디터 환경</b>에서만 이루어집니다.<br/>
        /// 메소드 탐색은 백그라운드 스레드풀에서 수행됩니다.
        /// </summary>
        /// <typeparam name="T">찾을 메소드에 정의된 <see cref="Attribute"/> 타입입니다.</typeparam>
        public static async UniTask InvokeDefinedMethods<T>() where T : Attribute
        {
            // Linq 쓰면 코드가 몇배는 깔끔해지겠지만, 메소드 호출이 너무 길어져 대략 2배에서 심하면 10배까지도 성능적인 차이가 나는것을 확인했습니다.
            // 소스 제너레이터를 사용해도 되지만, 모딩 환경을 고려하여 리플렉션으로 결정했습니다.
            List<MethodInfo> methods = new();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            await UniTask.RunOnThreadPool(() =>
            {
                for (int i = 0; i < types.Count; i++)
                {
                    foreach (var method in types[i].GetRuntimeMethods())
                    {
                        if (!method.IsStatic || method.IsSpecialName || method.IsAbstract)
                            return;
                        
                        if (method.IsDefined(typeof(T)))
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            if (!method.GetParameters().IsEmpty())
                            {
                                Debug.LogWarning
                                (
                                    $"A method {method.DeclaringType?.Name}.{method.Name} defined as an attribute has been found with a non-zero number of parameters.\n" +
                                    "This is currently ignored, but the built program will not check the number of parameters, so this will cause problems!"
                                );
                                return;
                            }
                            else if (!method.IsDefined(typeof(PreserveAttribute)))
                                Debug.LogWarning($"The method {method.DeclaringType?.Name}.{method.Name} is invoked via '{nameof(InvokeDefinedMethods)}' but may be subject to code stripping during build.\nConsider adding the 'Preserve' attribute to prevent this method from being removed.");
#endif
                            methods.Add(method);
                        }
                    }
                }
            });
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"It took {stopwatch.Elapsed.TotalSeconds} seconds to create a list of methods that match the condition.", $"{nameof(ReflectionUtility)}.{nameof(InvokeDefinedMethods)}<{typeof(T).Name}>");
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
        
        public static bool HasDefaultConstructor(this Type t, bool nonPublic = false)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (nonPublic)
                flags |= BindingFlags.NonPublic; 
            
            return t.IsValueType || t.GetConstructor(flags, null, Type.EmptyTypes, null) != null;
        }
    }
}

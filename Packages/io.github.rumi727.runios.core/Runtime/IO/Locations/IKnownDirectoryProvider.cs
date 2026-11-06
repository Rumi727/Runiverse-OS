#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Providers;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Provides the well-known directories of the system, such as the home directory.<br/>
    /// 홈 디렉터리처럼 시스템에서 잘 알려진 디렉터리를 제공합니다.
    /// </summary>
    public interface IKnownDirectoryProvider : IService
    {
        /// <summary>
        /// Asynchronously enumerates the known directories.<br/>
        /// 알려진 디렉터리를 비동기로 열거합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous sequence containing the known directories.<br/>
        /// 알려진 디렉터리를 포함하는 비동기 시퀀스를 반환합니다.
        /// </returns>
        IUniTaskAsyncEnumerable<KnownDirectory> EnumerateDirectories();

        /// <summary>
        /// Asynchronously gets the first known directory that has the specified type.<br/>
        /// 지정된 타입을 가진 첫 번째 알려진 디렉터리를 비동기로 가져옵니다.
        /// </summary>
        /// <param name="type">
        /// The type of the known directory to get.<br/>
        /// 가져올 알려진 디렉터리의 타입입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the first matching directory, or <see langword="null"/> when no directory has the type.<br/>
        /// 비동기 작업이 완료되면 일치하는 첫 번째 디렉터리를 반환하며, 해당 타입의 디렉터리가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        async UniTask<KnownDirectory?> TryGetDirectory(KnownDirectoryType type)
        {
            await foreach (var item in EnumerateDirectories())
            {
                if (type != item.type)
                    continue;

                return item;
            }

            return null;
        }
    }
}
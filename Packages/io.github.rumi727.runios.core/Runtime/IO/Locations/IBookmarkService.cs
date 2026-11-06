#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Provides the bookmarked directories of the system.<br/>
    /// 시스템에 즐겨찾기로 등록된 디렉터리를 제공합니다.
    /// </summary>
    public interface IBookmarkService : IService
    {
        /// <summary>
        /// Asynchronously enumerates the bookmarks.<br/>
        /// 즐겨찾기를 비동기로 열거합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous sequence containing the bookmarks.<br/>
        /// 즐겨찾기를 포함하는 비동기 시퀀스를 반환합니다.
        /// </returns>
        IUniTaskAsyncEnumerable<Bookmark> EnumerateBookmarks();
    }
}

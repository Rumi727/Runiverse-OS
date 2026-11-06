#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Services;
using RuniOS.Resource;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Provides the local directory bookmarks defined in the GTK <c>bookmarks</c> file and the KDE <c>user-places.xbel</c> file.<br/>
    /// GTK <c>bookmarks</c> 파일과 KDE <c>user-places.xbel</c> 파일에 정의된 로컬 디렉터리 즐겨찾기를 제공합니다.
    /// </summary>
    public sealed partial class LinuxBookmarkService : IBookmarkService
    {
        const string fileScheme = "file://";

        LinuxBookmarkService() { }

        /// <summary>
        /// Gets the Linux bookmark service instance.<br/>
        /// Linux 즐겨찾기 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static LinuxBookmarkService instance { get; } = new();

        /// <summary>
        /// The identifier of the Linux bookmark service.<br/>
        /// Linux 즐겨찾기 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "bookmark/linux");
        Identifier IService.id => id;

        /// <summary>
        /// Enumerates the <c>file://</c> bookmarks of the GTK <c>gtk-3.0/bookmarks</c> file and the KDE <c>user-places.xbel</c> file. Bookmarks of other schemes and KDE system items are skipped, and a path is returned only once.<br/>
        /// GTK <c>gtk-3.0/bookmarks</c> 파일과 KDE <c>user-places.xbel</c> 파일의 <c>file://</c> 즐겨찾기를 열거합니다. 다른 스킴의 즐겨찾기와 KDE 시스템 항목은 건너뛰고, 같은 경로는 한 번만 반환합니다.
        /// </summary>
        /// <returns>
        /// A sequence containing the bookmarks, or an empty sequence when no file can be read.<br/>
        /// 즐겨찾기를 포함하는 시퀀스를 반환하며, 읽을 수 있는 파일이 없으면 빈 시퀀스를 반환합니다.
        /// </returns>
        public static IEnumerable<Bookmark> EnumerateBookmarks()
        {
            HashSet<PhysicalPath> paths = new();
            foreach (Bookmark bookmark in ReadGtkBookmarks().Concat(ReadKdeBookmarks()))
            {
                if (paths.Add(bookmark.path))
                    yield return bookmark;
            }
        }

        IUniTaskAsyncEnumerable<Bookmark> IBookmarkService.EnumerateBookmarks() => EnumerateBookmarks().ToUniTaskAsyncEnumerable();

        static string GetHomeBasedDirectory(string environmentVariable, string relativePath)
        {
            string? directory = Environment.GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(directory) || !Path.IsPathRooted(directory))
                directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), relativePath);

            return directory;
        }

        static List<Bookmark> ReadGtkBookmarks()
        {
            List<Bookmark> result = new();

            try
            {
                foreach (string line in File.ReadLines(Path.Combine(GetHomeBasedDirectory("XDG_CONFIG_HOME", ".config"), "gtk-3.0", "bookmarks")))
                {
                    if (TryParseGtkBookmark(line, out Bookmark bookmark))
                        result.Add(bookmark);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            return result;
        }

        static List<Bookmark> ReadKdeBookmarks()
        {
            List<Bookmark> result = new();

            try
            {
                XDocument document = XDocument.Load(Path.Combine(GetHomeBasedDirectory("XDG_DATA_HOME", ".local/share"), "user-places.xbel"));
                foreach (XElement element in document.Descendants("bookmark"))
                {
                    // 시스템 항목은 알려진 디렉터리와 겹칩니다.
                    if (element.Descendants("isSystemItem").Any(x => x.Value == "true"))
                        continue;

                    string? href = (string?)element.Attribute("href");
                    if (href != null && TryCreateBookmark(href, element.Element("title")?.Value ?? string.Empty, out Bookmark bookmark))
                        result.Add(bookmark);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            catch (XmlException) { }

            return result;
        }

        // GTK 한 줄은 "URI [이름]" 형식입니다.
        static bool TryParseGtkBookmark(string line, out Bookmark bookmark)
        {
            string trimmed = line.Trim();
            int spaceIndex = trimmed.IndexOf(' ');

            return TryCreateBookmark(spaceIndex < 0 ? trimmed : trimmed[..spaceIndex], spaceIndex < 0 ? string.Empty : trimmed[(spaceIndex + 1)..].Trim(), out bookmark);
        }

        // 이름이 비어 있으면 경로의 마지막 요소를 이름으로 씁니다.
        static bool TryCreateBookmark(string uri, string name, out Bookmark bookmark)
        {
            bookmark = default;

            if (!uri.StartsWith(fileScheme, StringComparison.Ordinal))
                return false;

            string path = Uri.UnescapeDataString(uri[fileScheme.Length..]);
            if (!Path.IsPathRooted(path))
                return false;

            try
            {
                PhysicalPath physicalPath = (PhysicalPath)path;
                if (name.Length == 0)
                    name = Path.GetFileName(path.TrimEnd('/'));

                bookmark = new Bookmark(name, physicalPath);
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (IOException) { return false; }
            catch (NotSupportedException) { return false; }
        }

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
#endif
    }
}

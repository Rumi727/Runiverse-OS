#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Git
{
    /// <summary>
    /// Identifies a repository package path; revision selection belongs to requirements.<br/>
    /// 저장소의 패키지 경로를 식별하며 리비전 선택은 요구사항에 속합니다.
    /// </summary>
    public sealed class GitPackageSource : PackageSource
    {
        /// <inheritdoc/>
        public const string sourceKind = "upm:git";
        /// <summary>
        /// Gets the canonical Git repository URI.<br/>
        /// 정규화된 Git 저장소 URI를 가져옵니다.
        /// </summary>
        public string repositoryUrl { get; }
        /// <summary>
        /// Gets the repository-relative package directory.<br/>
        /// 저장소 상대 패키지 디렉터리를 가져옵니다.
        /// </summary>
        public string packagePath { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="repositoryUrl">
        /// An absolute Git URI without UPM query or fragment syntax.<br/>
        /// UPM query 또는 fragment 구문을 제외한 절대 Git URI입니다.
        /// </param>
        /// <param name="packagePath">
        /// The repository-relative package directory; empty selects the repository root.<br/>
        /// 저장소 상대 패키지 디렉터리이며 빈 값은 저장소 루트를 선택합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public GitPackageSource(string repositoryUrl, string packagePath = "") : base(sourceKind, Canonical(repositoryUrl) + "?path=/" + NormalizePath(packagePath))
        {
            this.repositoryUrl = Canonical(repositoryUrl); this.packagePath = NormalizePath(packagePath);
        }
        internal static string NormalizePath(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            path = path.Replace('\\', '/').Trim('/');
            if (path.Split('/').Any(x => x == ".." || x == ".") || path.Any(x => x < 32 || x == '?' || x == '#' || x == ':'))
                throw new ArgumentException("A repository-relative package path without traversal is required.", nameof(path));
            return string.Join("/", path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
        }
        internal static string Canonical(string url)
        {
            AdapterData.Text(url, nameof(url));
            if (url.StartsWith("git+", StringComparison.Ordinal)) url = url.Substring(4);
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri is null ||
                !new[] { "https", "http", "ssh", "git", "file" }.Contains(uri.Scheme) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                ((uri.Scheme == "http" || uri.Scheme == "https") && !string.IsNullOrEmpty(uri.UserInfo)))
                throw new ArgumentException("An absolute Git URI without query, fragment or HTTP credentials is required.", nameof(url));
            return uri.AbsoluteUri.TrimEnd('/');
        }
        internal static GitPackageSource ParseDependency(string value, out string reference)
        {
            int hash = value.LastIndexOf('#'); reference = hash < 0 ? "HEAD" : value.Substring(hash + 1);
            string location = hash < 0 ? value : value.Substring(0, hash);
            int query = location.IndexOf("?path=", StringComparison.Ordinal);
            return new GitPackageSource(query < 0 ? location : location.Substring(0, query), query < 0 ? "" : Uri.UnescapeDataString(location.Substring(query + 6)));
        }
    }
}

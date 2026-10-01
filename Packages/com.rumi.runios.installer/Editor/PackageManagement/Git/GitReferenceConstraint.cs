#nullable enable
using System;
using System.Linq;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Unity.Editor.Internal;

namespace RuniOS.PackageManagement.Unity.Editor.Git
{
    /// <summary>
    /// Requests a Git commit, branch, tag or HEAD resolved to a commit before graph publication.<br/>
    /// 그래프 제공 전에 commit으로 확정할 Git commit, branch, tag 또는 HEAD를 요구합니다.
    /// </summary>
    public sealed class GitReferenceConstraint : PackageConstraint
    {
        /// <inheritdoc/>
        public const string constraintKind = "upm:git-reference";
        /// <summary>
        /// Gets the Git reference to resolve before selecting a commit.<br/>
        /// commit 선택 전에 해석할 Git reference를 가져옵니다.
        /// </summary>
        public string reference { get; }
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="reference">
        /// A fetchable branch, tag, HEAD or complete commit hash.<br/>
        /// fetch 가능한 branch, tag, HEAD 또는 전체 commit hash입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public GitReferenceConstraint(string reference) : base(constraintKind)
        {
            this.reference = AdapterData.Text(reference, nameof(reference));
            if (reference.StartsWith("-", StringComparison.Ordinal) || reference.Any(char.IsWhiteSpace) || reference.Any(x => x < 32) ||
                reference.Contains(":") || reference.Contains("~") || reference.Contains("^") || reference.Contains("..") || reference.Contains("\\") || reference.Contains("@{"))
                throw new ArgumentException("A fetchable Git reference or full commit hash is required.", nameof(reference));
        }
    }
}

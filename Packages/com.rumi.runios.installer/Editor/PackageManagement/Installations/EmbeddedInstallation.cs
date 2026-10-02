#nullable enable
using System;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Requires an existing embedded package without acquiring or changing its contents.<br/>
    /// 내용을 획득하거나 변경하지 않고 기존 embedded package를 요구합니다.
    /// </summary>
    public sealed class EmbeddedInstallation : IInstallation
    {
        /// <summary>
        /// Gets the required native package name.<br/>
        /// 필요한 native package 이름을 가져옵니다.
        /// </summary>
        public string packageName { get; }
        /// <summary>
        /// Creates an embedded existence requirement.<br/>
        /// embedded 존재 요구사항을 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the name is empty or whitespace.<br/>
        /// 이름이 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public EmbeddedInstallation(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName)) throw new ArgumentException("An embedded package name is required.", nameof(packageName));
            this.packageName = packageName;
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using UnityEditorInternal;

namespace RuniOS.PackageManagement.Unity
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
        /// Gets required assembly-definition assets, preserving missing reference slots.<br/>
        /// 누락된 참조 슬롯을 유지하며 필수 어셈블리 정의 에셋을 가져옵니다.
        /// </summary>
        public IReadOnlyList<AssemblyDefinitionAsset?> requiredAssemblyReferences { get; }
        /// <summary>
        /// Creates an embedded existence requirement.<br/>
        /// embedded 존재 요구사항을 생성합니다.
        /// </summary>
        /// <param name="packageName">
        /// The native package name.<br/>
        /// native package 이름입니다.
        /// </param>
        /// <param name="requiredAssemblyReferences">
        /// Required assembly-definition assets; <see langword="null"/> requires none, and missing entries are unmet requirements.<br/>
        /// 필수 어셈블리 정의 에셋이며 <see langword="null"/>이면 요구하지 않고 누락된 항목은 미충족 요구사항입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the name is empty or whitespace.<br/>
        /// 이름이 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public EmbeddedInstallation(string packageName, IEnumerable<AssemblyDefinitionAsset?>? requiredAssemblyReferences = null)
        {
            if (string.IsNullOrWhiteSpace(packageName)) throw new ArgumentException("An embedded package name is required.", nameof(packageName));
            this.packageName = packageName;
            this.requiredAssemblyReferences = requiredAssemblyReferences is null
                ? Array.Empty<AssemblyDefinitionAsset?>() : new List<AssemblyDefinitionAsset?>(requiredAssemblyReferences).AsReadOnly();
        }
    }
}

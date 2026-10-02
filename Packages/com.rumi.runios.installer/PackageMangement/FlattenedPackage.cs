#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Describes a definition's root selection and direct incoming dependency references.<br/>
    /// 정의의 root 선택 여부와 직접 들어오는 dependency 참조를 표현합니다.
    /// </summary>
    public readonly record struct FlattenedPackage
    {
        /// <summary>
        /// Gets the actual package definition reached during flattening.<br/>
        /// flatten 중 도달한 실제 package 정의를 가져옵니다.
        /// </summary>
        public IPackage package { get; }
        /// <summary>
        /// Gets whether the caller directly selected this definition.<br/>
        /// caller가 이 정의를 직접 선택했는지 여부를 가져옵니다.
        /// </summary>
        public bool isRoot { get; }
        /// <summary>
        /// Gets the distinct definitions that directly require this package.<br/>
        /// 이 package를 직접 요구하는 중복 없는 정의들을 가져옵니다.
        /// </summary>
        public IEnumerable<IPackage> requiredBy { get; }
        /// <summary>
        /// Captures provenance without making root selection and dependency usage mutually exclusive.<br/>
        /// root 선택과 dependency 사용을 상호 배타적으로 취급하지 않고 provenance를 보관합니다.
        /// </summary>
        /// <param name="package">
        /// The reached definition.<br/>
        /// 도달한 정의입니다.
        /// </param>
        /// <param name="isRoot">
        /// Whether the definition is a selected root.<br/>
        /// 선택한 root인지 여부입니다.
        /// </param>
        /// <param name="requiredBy">
        /// The direct dependents, deduplicated by definition instance during flattening.<br/>
        /// flatten 중 정의 인스턴스로 중복 제거한 직접 의존 package들입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="package"/> or <paramref name="requiredBy"/> is <see langword="null"/>.<br/>
        /// <paramref name="package"/> 또는 <paramref name="requiredBy"/>가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public FlattenedPackage(IPackage package, bool isRoot, IEnumerable<IPackage> requiredBy)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.isRoot = isRoot;
            this.requiredBy = Array.AsReadOnly((requiredBy ?? throw new ArgumentNullException(nameof(requiredBy))).ToArray());
        }
    }
}

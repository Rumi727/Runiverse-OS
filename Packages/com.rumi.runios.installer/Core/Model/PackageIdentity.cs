#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Identifies a selected revision at a canonical source location.<br/>
    /// 정규화된 출처 위치에서 선택한 리비전을 식별합니다.
    /// </summary>
    public sealed class PackageIdentity : IEquatable<PackageIdentity>
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId id { get; }
        /// <summary>
        /// Gets the capability key for the canonical source.<br/>
        /// 정규화된 출처의 기능 키를 가져옵니다.
        /// </summary>
        public string sourceKind { get; }
        /// <summary>
        /// Gets the canonical package location within the source kind.<br/>
        /// 출처 종류 내에서 정규화된 패키지 위치를 가져옵니다.
        /// </summary>
        public string sourceKey { get; }
        /// <summary>
        /// Gets the exact opaque revision; symbolic references must already be resolved.<br/>
        /// 정확한 리비전을 가져오며 기호 참조는 이미 해석된 상태여야 합니다.
        /// </summary>
        public string revision { get; }
        /// <summary>
        /// Gets optional version metadata without changing revision identity equality.<br/>
        /// 리비전 식별자의 동등성을 변경하지 않는 선택적 버전 메타데이터를 가져옵니다.
        /// </summary>
        public string? version { get; }

        /// <summary>
        /// Captures exact revision identity and optional descriptive version metadata.<br/>
        /// 정확한 리비전 식별자와 선택적 설명용 버전 메타데이터를 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="sourceKind">
        /// The canonical source capability key.<br/>
        /// 정규화된 출처 기능 키입니다.
        /// </param>
        /// <param name="sourceKey">
        /// The canonical package location within the source.<br/>
        /// 출처 내의 정규화된 패키지 위치입니다.
        /// </param>
        /// <param name="revision">
        /// The exact opaque revision.<br/>
        /// 정확한 리비전입니다.
        /// </param>
        /// <param name="version">
        /// Optional descriptive version metadata.<br/>
        /// 선택적 설명용 버전 메타데이터입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the identifier is uninitialized or supplied source, revision, or version text is empty or whitespace.<br/>
        /// 식별자가 초기화되지 않았거나 제공한 출처, 리비전 또는 버전 문자열이 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public PackageIdentity(PackageId id, string sourceKind, string sourceKey, string revision, string? version = null)
        {
            this.id = Snapshots.Id(id, nameof(id));
            this.sourceKind = Snapshots.Text(sourceKind, nameof(sourceKind));
            this.sourceKey = Snapshots.Text(sourceKey, nameof(sourceKey));
            this.revision = Snapshots.Text(revision, nameof(revision));
            this.version = version is null ? null : Snapshots.Text(version, nameof(version));
        }

        /// <inheritdoc/>
        public bool Equals(PackageIdentity? other) => other is not null && id == other.id &&
            sourceKind == other.sourceKind && sourceKey == other.sourceKey && revision == other.revision;
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PackageIdentity other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(id, sourceKind, sourceKey, revision);
    }
}

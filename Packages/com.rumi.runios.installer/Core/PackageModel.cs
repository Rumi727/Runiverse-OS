#nullable enable
using System;
using System.Collections.Generic;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Identifies a logical package independently of its source.<br/>
    /// 출처와 독립적으로 논리적 패키지를 식별합니다.
    /// </summary>
    public readonly struct PackageId : IEquatable<PackageId>
    {
        readonly string? value;
        /// <summary>
        /// Gets the identifier text; an uninitialized identifier returns an empty string.<br/>
        /// 식별자 문자열을 가져오며 초기화하지 않은 식별자는 빈 문자열을 반환합니다.
        /// </summary>
        public string Value => value ?? string.Empty;

        /// <summary>
        /// Validates non-empty logical package text.<br/>
        /// 비어 있지 않은 논리적 패키지 문자열을 검증합니다.
        /// </summary>
        /// <param name="value">
        /// The immutable value or identifier to retain.<br/>
        /// 보존할 불변 값 또는 식별자입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> is empty or whitespace.<br/>
        /// <paramref name="value"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public PackageId(string value) => this.value = Snapshots.Text(value, nameof(value));

        /// <inheritdoc/>
        public bool Equals(PackageId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PackageId other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        /// <inheritdoc/>
        public override string ToString() => Value;
        /// <summary>
        /// Compares identifiers using ordinal text equality.<br/>
        /// 문자열의 ordinal 동등성으로 식별자를 비교합니다.
        /// </summary>
        /// <param name="left">
        /// The first identifier.<br/>
        /// 첫 번째 식별자입니다.
        /// </param>
        /// <param name="right">
        /// The second identifier.<br/>
        /// 두 번째 식별자입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when equal; otherwise, <see langword="false"/>.<br/>
        /// 같으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator ==(PackageId left, PackageId right) => left.Equals(right);
        /// <summary>
        /// Checks whether identifiers differ using ordinal text comparison.<br/>
        /// 문자열의 ordinal 비교로 식별자가 다른지 확인합니다.
        /// </summary>
        /// <param name="left">
        /// The first identifier.<br/>
        /// 첫 번째 식별자입니다.
        /// </param>
        /// <param name="right">
        /// The second identifier.<br/>
        /// 두 번째 식별자입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when different; otherwise, <see langword="false"/>.<br/>
        /// 다르면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator !=(PackageId left, PackageId right) => !left.Equals(right);
    }

    /// <summary>
    /// Describes a canonical source location; subclasses must keep all payloads immutable.<br/>
    /// 정규화된 출처 위치를 나타내며 파생 클래스는 모든 데이터를 불변으로 유지해야 합니다.
    /// </summary>
    public abstract class PackageSource
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string Kind { get; }
        /// <summary>
        /// Gets the canonical location or configuration key used for descriptor comparison.<br/>
        /// 설명자 비교에 사용하는 정규화된 위치 또는 설정 키를 가져옵니다.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Captures canonical source location and routing information.<br/>
        /// 정규화된 출처 위치와 라우팅 정보를 저장합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <param name="key">
        /// The canonical descriptor identity within its kind.<br/>
        /// 해당 종류 내의 정규화된 설명자 식별자입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> or <paramref name="key"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/> 또는 <paramref name="key"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected PackageSource(string kind, string key)
        {
            Kind = Snapshots.Text(kind, nameof(kind));
            Key = Snapshots.Text(key, nameof(key));
        }
    }

    /// <summary>
    /// Describes a selection constraint interpreted by a registered evaluator.<br/>
    /// 등록된 평가기가 해석하는 선택 제약을 나타냅니다.
    /// </summary>
    public abstract class PackageConstraint
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// Assigns selection semantics to a registered evaluator key.<br/>
        /// 선택 의미를 등록된 평가기 키에 할당합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected PackageConstraint(string kind) => Kind = Snapshots.Text(kind, nameof(kind));
    }

    /// <summary>
    /// Requires an exact opaque revision without interpreting its version syntax.<br/>
    /// 버전 문법을 해석하지 않고 정확한 리비전을 요구합니다.
    /// </summary>
    public sealed class ExactRevisionConstraint : PackageConstraint
    {
        /// <summary>
        /// Identifies the built-in opaque exact-revision evaluator.<br/>
        /// 기본 정확한 리비전 평가기를 식별합니다.
        /// </summary>
        public const string ConstraintKind = "package:exact-revision";
        /// <summary>
        /// Gets the exact opaque revision; symbolic references must already be resolved.<br/>
        /// 정확한 리비전을 가져오며 기호 참조는 이미 해석된 상태여야 합니다.
        /// </summary>
        public string Revision { get; }

        /// <summary>
        /// Captures an exact revision without parsing its format.<br/>
        /// 형식을 해석하지 않고 정확한 리비전을 저장합니다.
        /// </summary>
        /// <param name="revision">
        /// The exact opaque revision.<br/>
        /// 정확한 리비전입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="revision"/> is empty or whitespace.<br/>
        /// <paramref name="revision"/>이 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public ExactRevisionConstraint(string revision) : base(ConstraintKind) =>
            Revision = Snapshots.Text(revision, nameof(revision));
    }

    /// <summary>
    /// Describes content that an acquisition provider can obtain; payloads must be immutable.<br/>
    /// 획득 프로바이더가 가져올 수 있는 콘텐츠를 나타내며 데이터는 불변이어야 합니다.
    /// </summary>
    public abstract class PackageArtifactReference
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// Assigns passive content data to an acquisition capability.<br/>
        /// 수동적 콘텐츠 데이터를 획득 기능에 할당합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected PackageArtifactReference(string kind) => Kind = Snapshots.Text(kind, nameof(kind));
    }

    /// <summary>
    /// Describes an installation representation independently of a package source.<br/>
    /// 패키지 출처와 독립적으로 설치 표현을 나타냅니다.
    /// </summary>
    public abstract class PackageInstallation
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string Kind { get; }
        /// <summary>
        /// Gets the canonical location or configuration key used for descriptor comparison.<br/>
        /// 설명자 비교에 사용하는 정규화된 위치 또는 설정 키를 가져옵니다.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Captures installation routing and configuration identity.<br/>
        /// 설치 라우팅 및 설정 식별자를 저장합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <param name="key">
        /// The canonical descriptor identity within its kind.<br/>
        /// 해당 종류 내의 정규화된 설명자 식별자입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> or <paramref name="key"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/> 또는 <paramref name="key"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected PackageInstallation(string kind, string key)
        {
            Kind = Snapshots.Text(kind, nameof(kind));
            Key = Snapshots.Text(key, nameof(key));
        }
    }

    /// <summary>
    /// Identifies an execution environment without containing environment services.<br/>
    /// 환경 서비스를 포함하지 않고 실행 환경을 식별합니다.
    /// </summary>
    public abstract class InstallationTarget
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string Kind { get; }
        /// <summary>
        /// Gets the canonical location or configuration key used for descriptor comparison.<br/>
        /// 설명자 비교에 사용하는 정규화된 위치 또는 설정 키를 가져옵니다.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Captures an environment location independently of environment services.<br/>
        /// 환경 서비스와 독립적으로 환경 위치를 저장합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <param name="key">
        /// The canonical descriptor identity within its kind.<br/>
        /// 해당 종류 내의 정규화된 설명자 식별자입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> or <paramref name="key"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/> 또는 <paramref name="key"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected InstallationTarget(string kind, string key)
        {
            Kind = Snapshots.Text(kind, nameof(kind));
            Key = Snapshots.Text(key, nameof(key));
        }
    }

    /// <summary>
    /// Locates a declaration using a document identifier and an adapter-defined position.<br/>
    /// 문서 식별자와 어댑터가 정의한 위치로 선언 위치를 나타냅니다.
    /// </summary>
    public sealed class DeclarationLocation
    {
        /// <summary>
        /// Gets the document identifier supplied by the declaration adapter.<br/>
        /// 선언 어댑터가 제공한 문서 식별자를 가져옵니다.
        /// </summary>
        public string Document { get; }
        /// <summary>
        /// Gets the adapter-defined location within the document.<br/>
        /// 어댑터가 정의한 문서 내 위치를 가져옵니다.
        /// </summary>
        public string Position { get; }

        /// <summary>
        /// Captures a document location without interpreting its format.<br/>
        /// 형식을 해석하지 않고 문서 위치를 저장합니다.
        /// </summary>
        /// <param name="document">
        /// The document identifier.<br/>
        /// 문서 식별자입니다.
        /// </param>
        /// <param name="position">
        /// The adapter-defined position, which may be empty.<br/>
        /// 빈 문자열일 수 있는 어댑터 정의 위치입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="document"/> is empty or whitespace.<br/>
        /// <paramref name="document"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public DeclarationLocation(string document, string position)
        {
            Document = Snapshots.Text(document, nameof(document));
            Position = position ?? throw new ArgumentNullException(nameof(position));
        }
    }

    /// <summary>
    /// Captures a requested package, its constraints, and an overridable source hint.<br/>
    /// 요청한 패키지, 제약 및 재지정 가능한 출처 힌트를 저장합니다.
    /// </summary>
    public sealed class PackageRequirement
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId Id { get; }
        /// <summary>
        /// Gets the default source that explicit resolution bindings may override.<br/>
        /// 명시적 해석 바인딩으로 재지정할 수 있는 기본 출처를 가져옵니다.
        /// </summary>
        public PackageSource? SourceHint { get; }
        /// <summary>
        /// Gets the immutable selection constraints that must all be satisfied.<br/>
        /// 모두 만족해야 하는 불변 선택 제약을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageConstraint> Constraints { get; }
        /// <summary>
        /// Gets the optional location of the original declaration.<br/>
        /// 원본 선언의 선택적 위치를 가져옵니다.
        /// </summary>
        public DeclarationLocation? Location { get; }

        /// <summary>
        /// Snapshots package intent while retaining its original declaration location.<br/>
        /// 원본 선언 위치를 유지하면서 패키지 의도를 스냅샷으로 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="sourceHint">
        /// The default source, overridable by explicit resolution bindings.<br/>
        /// 명시적 해석 바인딩으로 재지정할 수 있는 기본 출처입니다.
        /// </param>
        /// <param name="constraints">
        /// Selection constraints; omitted values produce an empty snapshot.<br/>
        /// 선택 제약이며 생략하면 빈 스냅샷을 생성합니다.
        /// </param>
        /// <param name="location">
        /// The optional original declaration location.<br/>
        /// 선택적 원본 선언 위치입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="id"/> is uninitialized or <paramref name="constraints"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="id"/>가 초기화되지 않았거나 <paramref name="constraints"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PackageRequirement(PackageId id, PackageSource? sourceHint = null,
            IEnumerable<PackageConstraint>? constraints = null, DeclarationLocation? location = null)
        {
            Id = Snapshots.Id(id, nameof(id));
            SourceHint = sourceHint;
            Constraints = Snapshots.List(constraints ?? Array.Empty<PackageConstraint>());
            Location = location;
        }
    }

    /// <summary>
    /// Associates a dependency declaration with its owning package metadata.<br/>
    /// 의존성 선언을 소유 패키지의 메타데이터와 연결합니다.
    /// </summary>
    public sealed class PackageDependency
    {
        /// <summary>
        /// Gets the original dependency requirement.<br/>
        /// 원본 의존성 요구를 가져옵니다.
        /// </summary>
        public PackageRequirement Requirement { get; }

        /// <summary>
        /// Retains an immutable dependency declaration.<br/>
        /// 불변 의존성 선언을 보존합니다.
        /// </summary>
        /// <param name="requirement">
        /// The immutable dependency declaration.<br/>
        /// 불변 의존성 선언입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public PackageDependency(PackageRequirement requirement) =>
            Requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
    }

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
        public PackageId Id { get; }
        /// <summary>
        /// Gets the capability key for the canonical source.<br/>
        /// 정규화된 출처의 기능 키를 가져옵니다.
        /// </summary>
        public string SourceKind { get; }
        /// <summary>
        /// Gets the canonical package location within the source kind.<br/>
        /// 출처 종류 내에서 정규화된 패키지 위치를 가져옵니다.
        /// </summary>
        public string SourceKey { get; }
        /// <summary>
        /// Gets the exact opaque revision; symbolic references must already be resolved.<br/>
        /// 정확한 리비전을 가져오며 기호 참조는 이미 해석된 상태여야 합니다.
        /// </summary>
        public string Revision { get; }
        /// <summary>
        /// Gets optional version metadata without changing revision identity equality.<br/>
        /// 리비전 식별자의 동등성을 변경하지 않는 선택적 버전 메타데이터를 가져옵니다.
        /// </summary>
        public string? Version { get; }

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
            Id = Snapshots.Id(id, nameof(id));
            SourceKind = Snapshots.Text(sourceKind, nameof(sourceKind));
            SourceKey = Snapshots.Text(sourceKey, nameof(sourceKey));
            Revision = Snapshots.Text(revision, nameof(revision));
            Version = version is null ? null : Snapshots.Text(version, nameof(version));
        }

        /// <inheritdoc/>
        public bool Equals(PackageIdentity? other) => other is not null && Id == other.Id &&
            SourceKind == other.SourceKind && SourceKey == other.SourceKey && Revision == other.Revision;
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PackageIdentity other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Id, SourceKind, SourceKey, Revision);
    }

    /// <summary>
    /// Provides an immutable candidate identity and content references.<br/>
    /// 불변 후보 식별자와 콘텐츠 참조를 제공합니다.
    /// </summary>
    public sealed class PackageCandidate
    {
        /// <summary>
        /// Gets the exact package identity; installed observations may report an unknown identity.<br/>
        /// 정확한 패키지 식별자를 가져오며 설치 관측은 알 수 없는 식별자를 보고할 수 있습니다.
        /// </summary>
        public PackageIdentity Identity { get; }
        /// <summary>
        /// Gets immutable content references without acquiring their content.<br/>
        /// 콘텐츠를 획득하지 않고 불변 콘텐츠 참조를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageArtifactReference> Artifacts { get; }

        /// <summary>
        /// Snapshots content references associated with an exact identity.<br/>
        /// 정확한 식별자와 연결된 콘텐츠 참조를 스냅샷으로 저장합니다.
        /// </summary>
        /// <param name="identity">
        /// The exact identity associated with the candidate.<br/>
        /// 후보와 연결된 정확한 식별자입니다.
        /// </param>
        /// <param name="artifacts">
        /// Immutable content references; omitted values produce an empty snapshot.<br/>
        /// 불변 콘텐츠 참조이며 생략하면 빈 스냅샷을 생성합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="artifacts"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="artifacts"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PackageCandidate(PackageIdentity identity, IEnumerable<PackageArtifactReference>? artifacts = null)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Artifacts = Snapshots.List(artifacts ?? Array.Empty<PackageArtifactReference>());
        }
    }

    /// <summary>
    /// Provides the dependency declarations retrieved for one exact candidate.<br/>
    /// 정확한 후보 하나에서 조회한 의존성 선언을 제공합니다.
    /// </summary>
    public sealed class PackageMetadata
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId Id { get; }
        /// <summary>
        /// Gets display text independent of frontend localization services.<br/>
        /// 프런트엔드 지역화 서비스와 독립적인 표시 문자열을 가져옵니다.
        /// </summary>
        public string DisplayName { get; }
        /// <summary>
        /// Gets optional version metadata without changing revision identity equality.<br/>
        /// 리비전 식별자의 동등성을 변경하지 않는 선택적 버전 메타데이터를 가져옵니다.
        /// </summary>
        public string? Version { get; }
        /// <summary>
        /// Gets immutable dependency declarations, edges, or effective observed identifiers.<br/>
        /// 불변 의존성 선언, 간선 또는 실제 관측된 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageDependency> Dependencies { get; }

        /// <summary>
        /// Snapshots dependency declarations from one exact candidate revision.<br/>
        /// 정확한 후보 리비전 하나의 의존성 선언을 스냅샷으로 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="dependencies">
        /// Dependency observations or declarations; omitted values produce an empty snapshot.<br/>
        /// 의존성 관측 또는 선언이며 생략하면 빈 스냅샷을 생성합니다.
        /// </param>
        /// <param name="displayName">
        /// Optional display text; omission uses the logical package identifier.<br/>
        /// 선택적 표시 문자열이며 생략하면 논리적 패키지 식별자를 사용합니다.
        /// </param>
        /// <param name="version">
        /// Optional descriptive version metadata.<br/>
        /// 선택적 설명용 버전 메타데이터입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the identifier is uninitialized, a dependency is <see langword="null"/>, or supplied version text is empty.<br/>
        /// 식별자가 초기화되지 않았거나 의존성이 <see langword="null"/>이거나 제공한 버전 문자열이 비어 있으면 발생합니다.
        /// </exception>
        public PackageMetadata(PackageId id, IEnumerable<PackageDependency>? dependencies = null, string? displayName = null, string? version = null)
        {
            Id = Snapshots.Id(id, nameof(id));
            DisplayName = displayName ?? id.Value;
            Version = version is null ? null : Snapshots.Text(version, nameof(version));
            Dependencies = Snapshots.List(dependencies ?? Array.Empty<PackageDependency>());
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
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
        public PackageId id { get; }
        /// <summary>
        /// Gets the default source that explicit resolution bindings may override.<br/>
        /// 명시적 해석 바인딩으로 재지정할 수 있는 기본 출처를 가져옵니다.
        /// </summary>
        public PackageSource? sourceHint { get; }
        /// <summary>
        /// Gets the immutable selection constraints that must all be satisfied.<br/>
        /// 모두 만족해야 하는 불변 선택 제약을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageConstraint> constraints { get; }
        /// <summary>
        /// Gets the optional location of the original declaration.<br/>
        /// 원본 선언의 선택적 위치를 가져옵니다.
        /// </summary>
        public DeclarationLocation? location { get; }

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
            this.id = Snapshots.Id(id, nameof(id));
            this.sourceHint = sourceHint;
            this.constraints = Snapshots.List(constraints ?? Array.Empty<PackageConstraint>());
            this.location = location;
        }
    }
}

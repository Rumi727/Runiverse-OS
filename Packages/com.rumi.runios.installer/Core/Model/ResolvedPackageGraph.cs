#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Exposes a complete, acyclic selection graph constructed only by the resolver.<br/>
    /// 리졸버만 생성하는 완전하고 순환 없는 선택 그래프를 제공합니다.
    /// </summary>
    public sealed class ResolvedPackageGraph
    {
        /// <summary>
        /// Gets the immutable resolved nodes indexed by logical package identifier.<br/>
        /// 논리적 패키지 식별자로 인덱싱한 불변 해석 노드를 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, ResolvedPackage> packages { get; }
        /// <summary>
        /// Gets all retained root requirements used for this resolution.<br/>
        /// 이 해석에 사용하는 유지할 전체 루트 요구를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> roots { get; }
        /// <summary>
        /// Gets immutable dependency declarations, edges, or effective observed identifiers.<br/>
        /// 불변 의존성 선언, 간선 또는 실제 관측된 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<ResolvedDependency> dependencies { get; }
        /// <summary>
        /// Gets a deterministic order in which dependencies precede their dependents.<br/>
        /// 의존 패키지가 이를 사용하는 패키지보다 앞서는 결정적인 순서를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageId> dependencyOrder { get; }

        internal ResolvedPackageGraph(IEnumerable<ResolvedPackage> packages, IEnumerable<PackageRequirement> roots)
        {
            this.packages = Snapshots.Map(packages.Select(x => new KeyValuePair<PackageId, ResolvedPackage>(x.identity.id, x)));
            this.roots = Snapshots.List(roots);
            dependencies = Snapshots.List(this.packages.Values.OrderBy(x => x.identity.id.value, StringComparer.Ordinal)
                .SelectMany(x => x.metadata.dependencies.Select(y => new ResolvedDependency(x.identity.id, y.requirement))));
            GraphOrder.TryOrder(this.packages.Keys.OrderBy(x => x.value, StringComparer.Ordinal),
                id => this.packages[id].metadata.dependencies.Select(x => x.requirement.id), out IReadOnlyList<PackageId> order);
            dependencyOrder = order;
        }
    }
}

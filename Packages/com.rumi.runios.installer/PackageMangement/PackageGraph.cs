#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Indexes caller-supplied exact definitions and computes dependency closures.<br/>
    /// caller가 제공한 exact 정의를 색인하고 dependency closure를 계산합니다.
    /// </summary>
    public sealed class PackageGraph
    {
        sealed class PackageReferenceComparer : IEqualityComparer<IPackage>
        {
            internal static readonly PackageReferenceComparer instance = new();
            public bool Equals(IPackage? x, IPackage? y) => ReferenceEquals(x, y);
            public int GetHashCode(IPackage obj) => RuntimeHelpers.GetHashCode(obj);
        }
        sealed class Node
        {
            internal readonly IPackage package;
            internal readonly PackageIdentity identity;
            internal readonly IPackage?[] dependencies;
            internal Node(IPackage package, PackageIdentity identity, IPackage?[] dependencies)
            { this.package = package; this.identity = identity; this.dependencies = dependencies; }
        }
        readonly struct Frame
        {
            internal readonly Node node;
            internal readonly int next;
            internal Frame(Node node, int next) { this.node = node; this.next = next; }
        }
        readonly Dictionary<IPackage, Node> _nodes = new(PackageReferenceComparer.instance);
        readonly List<PackageGraphDiagnostic> _definitionErrors = new();
        /// <summary>
        /// Captures definitions and dependencies, rejecting distinct instances with the same exact key.<br/>
        /// 정의와 dependencies를 보관하고 같은 exact 키의 서로 다른 인스턴스를 거부합니다.
        /// </summary>
        /// <param name="packages">
        /// The complete definition collection supplied by the caller.<br/>
        /// caller가 제공하는 전체 정의 collection입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when packages is null.<br/>
        /// packages가 null이면 발생합니다.
        /// </exception>
        public PackageGraph(IEnumerable<IPackage?> packages)
        {
            if (packages is null) throw new ArgumentNullException(nameof(packages));
            var keys = new Dictionary<PackageIdentity, IPackage>();
            int index = 0;
            foreach (IPackage? package in packages)
            {
                if (package is null)
                    _definitionErrors.Add(new PackageGraphDiagnostic("graph:missing-definition", "The input contains a missing definition.", referenceIndex: index));
                else if (!_nodes.ContainsKey(package))
                {
                    try
                    {
                        var identity = new PackageIdentity(package.id, package.exactIdentity);
                        IReadOnlyList<IPackage?> dependencies = package.dependencies ?? throw new InvalidOperationException("Dependencies cannot be null.");
                        var copy = new IPackage?[dependencies.Count];
                        for (int i = 0; i < copy.Length; i++) copy[i] = dependencies[i];
                        _nodes.Add(package, new Node(package, identity, copy));
                        if (keys.TryGetValue(identity, out IPackage? previous))
                            _definitionErrors.Add(new PackageGraphDiagnostic("graph:duplicate-definition", $"Distinct instances define '{identity}'.", package, previous, referenceIndex: index));
                        else keys.Add(identity, package);
                    }
                    catch (Exception exception)
                    {
                        _definitionErrors.Add(new PackageGraphDiagnostic("graph:invalid-definition", exception.Message, package, referenceIndex: index));
                    }
                }
                index++;
            }
        }
        /// <summary>
        /// Traverses selected roots, detecting missing references, cycles, and exact identity conflicts.<br/>
        /// 선택한 root를 탐색하고 참조 누락, 순환과 exact identity 충돌을 검출합니다.
        /// </summary>
        /// <param name="roots">
        /// Direct references to roots included in the supplied definition collection.<br/>
        /// 제공한 정의 collection에 포함된 root의 직접 참조입니다.
        /// </param>
        /// <returns>
        /// The unique closure and diagnostics; a failed result may contain a partial closure.<br/>
        /// 고유 closure와 diagnostic을 반환하며 실패한 결과에는 부분 closure가 포함될 수 있습니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when roots is null.<br/>
        /// roots가 null이면 발생합니다.
        /// </exception>
        public PackageFlattenResult Flatten(IEnumerable<IPackage?> roots)
        {
            if (roots is null) throw new ArgumentNullException(nameof(roots));
            var errors = new List<PackageGraphDiagnostic>(_definitionErrors);
            var packages = new List<IPackage>();
            var required = new HashSet<PackageIdentity>();
            var states = new Dictionary<Node, byte>();
            var selected = new Dictionary<PackageId, Node>();
            var parents = new Dictionary<Node, Node?>();
            var stack = new Stack<Frame>();
            var path = new List<IPackage>();
            int rootIndex = 0;
            foreach (IPackage? root in roots)
            {
                Push(root, null, rootIndex++);
                while (stack.Count != 0)
                {
                    Frame frame = stack.Pop();
                    if (frame.next < frame.node.dependencies.Length)
                    {
                        stack.Push(new Frame(frame.node, frame.next + 1));
                        Push(frame.node.dependencies[frame.next], frame.node.package, frame.next);
                    }
                    else
                    {
                        states[frame.node] = 2;
                        packages.Add(frame.node.package);
                        path.RemoveAt(path.Count - 1);
                    }
                }
            }
            return new PackageFlattenResult(packages, errors, required);

            void Push(IPackage? package, IPackage? owner, int referenceIndex)
            {
                if (package is null || !_nodes.TryGetValue(package, out Node? node) || node is null)
                {
                    errors.Add(new PackageGraphDiagnostic("graph:missing-reference", package is null ? "A package reference is missing." : "The referenced instance is outside the supplied definitions.", package, owner: owner, referenceIndex: referenceIndex, path: ErrorPath(package)));
                    return;
                }
                if (states.TryGetValue(node, out byte state))
                {
                    if (state == 1)
                        errors.Add(new PackageGraphDiagnostic("graph:cycle", $"A dependency cycle reaches '{node.identity}'.", package, owner: owner, referenceIndex: referenceIndex, path: ErrorPath(package)));
                    return;
                }
                if (selected.TryGetValue(node.identity.id, out Node? previous) && previous is not null && previous.identity != node.identity)
                    errors.Add(new PackageGraphDiagnostic("graph:identity-conflict", $"'{previous.identity}' conflicts with '{node.identity}'.", package, previous.package, owner, referenceIndex, ErrorPath(package), RelatedPath(previous)));
                else selected[node.identity.id] = node;
                parents[node] = owner is not null && _nodes.TryGetValue(owner, out Node? parent) ? parent : null;
                required.Add(node.identity);
                states[node] = 1;
                path.Add(package);
                stack.Push(new Frame(node, 0));
            }
            IPackage[] RelatedPath(Node node)
            {
                var result = new List<IPackage>();
                Node? current = node;
                while (current is not null)
                {
                    result.Add(current.package);
                    current = parents[current];
                }
                result.Reverse();
                return result.ToArray();
            }
            IPackage[] ErrorPath(IPackage? package)
            {
                var result = new IPackage[path.Count + (package is null ? 0 : 1)];
                path.CopyTo(result);
                if (package is not null) result[result.Length - 1] = package;
                return result;
            }
        }
    }
}

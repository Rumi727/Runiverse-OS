#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Computes exact-definition closures by following selected roots' dependency references.<br/>
    /// 선택한 root의 dependency 참조를 따라 exact 정의 closure를 계산합니다.
    /// </summary>
    public static class PackageGraph
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
            internal readonly List<IPackage> requiredBy = new();
            internal readonly HashSet<IPackage> dependents = new(PackageReferenceComparer.instance);
            internal bool isRoot;
            internal Node(IPackage package, PackageIdentity identity, IPackage?[] dependencies)
            { this.package = package; this.identity = identity; this.dependencies = dependencies; }
        }
        readonly struct Frame
        {
            internal readonly Node node;
            internal readonly int next;
            internal Frame(Node node, int next) { this.node = node; this.next = next; }
        }
        /// <summary>
        /// Traverses roots without a catalog, recording provenance and detecting invalid definitions, cycles, and identity conflicts.<br/>
        /// catalog 없이 root를 탐색하고 provenance를 기록하며 잘못된 정의, 순환과 identity 충돌을 검출합니다.
        /// </summary>
        /// <param name="roots">
        /// Direct references to selected roots; dependencies are followed from each definition.<br/>
        /// 선택한 root의 직접 참조이며 각 정의에서 dependency를 따라갑니다.
        /// </param>
        /// <returns>
        /// A closure with provenance and diagnostics in unspecified package order; failure may leave a partial closure.<br/>
        /// provenance와 diagnostic을 포함한 closure를 반환하며 package 순서는 정의되지 않고 실패 시 부분 closure가 남을 수 있습니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="roots"/> is <see langword="null"/>.<br/>
        /// <paramref name="roots"/>가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public static PackageFlattenResult Flatten(IEnumerable<IPackage?> roots)
        {
            if (roots is null) throw new ArgumentNullException(nameof(roots));
            var nodes = new Dictionary<IPackage, Node>(PackageReferenceComparer.instance);
            var invalid = new HashSet<IPackage>(PackageReferenceComparer.instance);
            var keys = new Dictionary<PackageIdentity, Node>();
            var errors = new List<PackageGraphDiagnostic>();
            var ordered = new List<Node>();
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
                        ordered.Add(frame.node);
                        path.RemoveAt(path.Count - 1);
                    }
                }
            }
            var packages = new List<FlattenedPackage>(ordered.Count);
            foreach (Node node in ordered)
                packages.Add(new FlattenedPackage(node.package, node.isRoot, node.requiredBy));
            return new PackageFlattenResult(packages, errors, required);

            void Push(IPackage? package, IPackage? owner, int referenceIndex)
            {
                if (package is null)
                {
                    errors.Add(new PackageGraphDiagnostic("graph:missing-reference", "A package reference is missing.", owner: owner, referenceIndex: referenceIndex, path: ErrorPath(null)));
                    return;
                }
                if (invalid.Contains(package)) return;
                if (!nodes.TryGetValue(package, out Node? node) || node is null)
                {
                    try
                    {
                        var identity = new PackageIdentity(package.id, package.exactIdentity);
                        IReadOnlyList<IPackage?> dependencies = package.dependencies ?? throw new InvalidOperationException("Dependencies cannot be null.");
                        var copy = new IPackage?[dependencies.Count];
                        for (int i = 0; i < copy.Length; i++) copy[i] = dependencies[i];
                        node = new Node(package, identity, copy);
                    }
                    catch (Exception exception)
                    {
                        invalid.Add(package);
                        errors.Add(new PackageGraphDiagnostic("graph:invalid-definition", exception.Message, package, owner: owner, referenceIndex: referenceIndex, path: ErrorPath(package)));
                        return;
                    }
                    nodes.Add(package, node);
                    if (keys.TryGetValue(node.identity, out Node? duplicate) && duplicate is not null)
                        errors.Add(new PackageGraphDiagnostic("graph:duplicate-definition", $"Distinct instances define '{node.identity}'.", package, duplicate.package, owner, referenceIndex, ErrorPath(package), RelatedPath(duplicate)));
                    else keys.Add(node.identity, node);
                }
                // Record every incoming edge, including edges to already visited nodes.
                if (owner is null) node.isRoot = true;
                else if (node.dependents.Add(owner)) node.requiredBy.Add(owner);
                if (states.TryGetValue(node, out byte state))
                {
                    if (state == 1)
                        errors.Add(new PackageGraphDiagnostic("graph:cycle", $"A dependency cycle reaches '{node.identity}'.", package, owner: owner, referenceIndex: referenceIndex, path: ErrorPath(package)));
                    return;
                }
                if (selected.TryGetValue(node.identity.id, out Node? previous) && previous is not null && previous.identity != node.identity)
                    errors.Add(new PackageGraphDiagnostic("graph:identity-conflict", $"'{previous.identity}' conflicts with '{node.identity}'.", package, previous.package, owner, referenceIndex, ErrorPath(package), RelatedPath(previous)));
                else selected[node.identity.id] = node;
                parents[node] = owner is not null && nodes.TryGetValue(owner, out Node? parent) ? parent : null;
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

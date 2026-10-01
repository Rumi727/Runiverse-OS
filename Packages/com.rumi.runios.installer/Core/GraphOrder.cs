#nullable enable
using System;
using System.Collections.Generic;

namespace RuniOS.PackageManagement
{
    static class GraphOrder
    {
        internal static bool TryOrder<T>(IEnumerable<T> nodes, Func<T, IEnumerable<T>> prerequisites,
            out IReadOnlyList<T> order) where T : notnull
            => TryOrder(nodes, prerequisites, out order, out _);

        internal static bool TryOrder<T>(IEnumerable<T> nodes, Func<T, IEnumerable<T>> prerequisites,
            out IReadOnlyList<T> order, out IReadOnlyList<T> cycle) where T : notnull
        {
            Dictionary<T, int> states = new();
            List<T> result = new();
            List<T> path = new();
            List<T> cyclePath = new();

            bool Visit(T node)
            {
                if (states.TryGetValue(node, out int state))
                {
                    if (state == 2)
                        return true;
                    int start = path.FindIndex(x => EqualityComparer<T>.Default.Equals(x, node));
                    cyclePath.AddRange(path.GetRange(start, path.Count - start));
                    cyclePath.Add(node);
                    return false;
                }
                states[node] = 1;
                path.Add(node);
                foreach (T prerequisite in prerequisites(node))
                {
                    if (!Visit(prerequisite))
                        return false;
                }
                states[node] = 2;
                path.RemoveAt(path.Count - 1);
                result.Add(node);
                return true;
            }

            foreach (T node in nodes)
            {
                if (!Visit(node))
                {
                    order = Array.Empty<T>();
                    cycle = Snapshots.List(cyclePath);
                    return false;
                }
            }
            order = Snapshots.List(result);
            cycle = Array.Empty<T>();
            return true;
        }

        internal static bool DependsOn<T>(T node, T prerequisite, Func<T, IEnumerable<T>> prerequisites)
            where T : notnull
        {
            HashSet<T> visited = new();
            Stack<T> pending = new();
            pending.Push(node);
            while (pending.Count > 0)
            {
                foreach (T next in prerequisites(pending.Pop()))
                {
                    if (EqualityComparer<T>.Default.Equals(next, prerequisite))
                        return true;
                    if (visited.Add(next))
                        pending.Push(next);
                }
            }
            return false;
        }
    }
}

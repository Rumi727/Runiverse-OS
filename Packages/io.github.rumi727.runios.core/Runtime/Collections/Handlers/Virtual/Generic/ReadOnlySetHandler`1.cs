#nullable enable
using System.Collections.ObjectModel;
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CollectionHandler(typeof(ReadOnlySet<>), useForChildren = true)]
    public class ReadOnlySetHandler(IEnumerable targetCollection) : VirtualListHandler(targetCollection);
}
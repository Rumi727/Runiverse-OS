#nullable enable
using RuniOS.Collections.Generic;
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CollectionHandler(typeof(ReadOnlySet<>), useForChildren = true)]
    public class ReadOnlySetHandler(IEnumerable targetCollection) : VirtualListHandler(targetCollection);
}
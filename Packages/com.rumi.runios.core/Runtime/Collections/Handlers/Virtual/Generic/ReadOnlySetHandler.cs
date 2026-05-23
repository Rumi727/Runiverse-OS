#nullable enable
using RuniOS.Collections.Generic;
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CustomCollectionHandler(typeof(ReadOnlySet<>))]
    public class ReadOnlySetHandler(IEnumerable targetCollection) : VirtualListHandler(targetCollection);
}
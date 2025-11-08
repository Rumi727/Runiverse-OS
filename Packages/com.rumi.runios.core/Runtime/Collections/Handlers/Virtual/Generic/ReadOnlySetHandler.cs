#nullable enable
using RuniOS.Collections.Generic;
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CustomCollectionHandler(typeof(ReadOnlySet<>))]
    public class ReadOnlySetHandler : VirtualListHandler
    {
        public ReadOnlySetHandler(IEnumerable targetCollection) : base(targetCollection) { }
    }
}

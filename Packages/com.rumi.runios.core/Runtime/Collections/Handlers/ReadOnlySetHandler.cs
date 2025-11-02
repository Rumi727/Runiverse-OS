#nullable enable
using RuniOS.Collections.Generic;
using System;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(ReadOnlySet<>))]
    public class ReadOnlySetHandler : IEnumerableHandler
    {
        public ReadOnlySetHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }
    }
}

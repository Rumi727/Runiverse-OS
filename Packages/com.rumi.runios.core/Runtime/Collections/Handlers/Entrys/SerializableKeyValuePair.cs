#nullable enable
using RuniOS.Collections.Generic;

namespace RuniOS.Collections.Handlers.Entrys;

[CustomEntryHandler(typeof(ISerializableKeyValuePair))]
[CustomCollectionHandler(typeof(SerializableKeyValuePair<,>))]
public class SerializableKeyValuePairHandler : EntryHandler
{
    public SerializableKeyValuePairHandler(object targetEntry) : base(targetEntry) { }

    protected override object? key => ((ISerializableKeyValuePair)targetEntry).Key;
    protected override object? value => ((ISerializableKeyValuePair)targetEntry).Value;

    public override object CreateInstance(object? key, object? value) => ((ISerializableKeyValuePair)targetEntry).CreateInstance(key, value);
}
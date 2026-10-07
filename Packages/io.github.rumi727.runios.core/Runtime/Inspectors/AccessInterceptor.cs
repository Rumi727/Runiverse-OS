#nullable enable
namespace RuniOS.Inspectors
{
    public class AccessInterceptor : ICloneable
    {
        public delegate object? ReadFunc(Func<object?> orgMethod);
        public delegate void WriteAction(object? value);

        public delegate IEnumerable<object?> GetValuesFunc(Func<bool, IEnumerable<object?>> orgMethod, bool noCopy = false);
        public delegate void SetValuesAction(IEnumerable<object?> values);

        public delegate bool IsReadableFunc(InspectorFlags flags = InspectorFlags.Public, bool noInstanceCheck = false);
        public delegate bool IsWritableFunc(InspectorFlags flags = InspectorFlags.Public, bool noInstanceCheck = false);

        public ReadFunc? readFunc { get; set; }
        public GetValuesFunc? getValuesFunc { get; set; }

        public WriteAction? writeAction { get; set; }
        public SetValuesAction? setValuesAction { get; set; }

        public IsReadableFunc? isReadableFunc { get; set; }
        public IsWritableFunc? isWritableFunc { get; set; }

        public AccessInterceptor Clone() => new AccessInterceptor
        {
            readFunc = readFunc,
            getValuesFunc = getValuesFunc,
            writeAction = writeAction,
            setValuesAction = setValuesAction,
            isReadableFunc = isReadableFunc,
            isWritableFunc = isWritableFunc
        };
        object ICloneable.Clone() => Clone();
    }
}
#nullable enable
using FMOD;
using System.Runtime.InteropServices;
using System.Text;

namespace RuniOS.Sounds.Processing.Custom
{
    /// <summary>
    /// Collects parameter declarations while a <see cref="CustomDSP"/> is being created.<br/>
    /// <see cref="CustomDSP"/>가 생성되는 동안 parameter 선언을 수집합니다.
    /// </summary>
    /// <remarks>
    /// Instances are supplied only to <see cref="CustomDSP.OnConfigureParameters(CustomDSPParameterBuilder)"/>.<br/>
    /// They cannot accept declarations after that callback returns.<br/><br/>
    /// 인스턴스는 <see cref="CustomDSP.OnConfigureParameters(CustomDSPParameterBuilder)"/>에만 전달됩니다.<br/>
    /// 해당 콜백이 반환된 뒤에는 선언을 추가할 수 없습니다.
    /// </remarks>
    [Obsolete("CustomDSP has not been tested and is quite complex!")]
    public sealed class CustomDSPParameterBuilder
    {
        readonly List<Definition> definitions = [];
        bool isSealed;

        internal CustomDSPParameterBuilder() { }

        /// <summary>
        /// Declares a floating-point parameter and returns its stable index.<br/>
        /// 부동 소수점 parameter를 선언하고 안정적인 index를 반환합니다.
        /// </summary>
        /// <param name="name">
        /// UTF-8 parameter name with at most 15 bytes.<br/>
        /// UTF-8 기준 최대 15 byte인 parameter 이름입니다.
        /// </param>
        /// <param name="min">
        /// Minimum accepted value.<br/>
        /// 허용할 최솟값입니다.
        /// </param>
        /// <param name="max">
        /// Maximum accepted value.<br/>
        /// 허용할 최댓값입니다.
        /// </param>
        /// <param name="defaultValue">
        /// Initial value within the declared range.<br/>
        /// 선언한 범위 안의 초기값입니다.
        /// </param>
        /// <param name="label">
        /// Optional UTF-8 unit label with at most 15 bytes.<br/>
        /// UTF-8 기준 최대 15 byte인 선택적 단위 label입니다.
        /// </param>
        /// <param name="description">
        /// Optional parameter description.<br/>
        /// 선택적 parameter 설명입니다.
        /// </param>
        /// <returns>
        /// Stable parameter index.<br/>
        /// 안정적인 parameter index를 반환합니다.
        /// </returns>
        public int AddFloat(string name, float min, float max, float defaultValue, string label = "", string description = "")
        {
            ValidateRange(name, min, max, defaultValue);
            ValidateMetadata(label, description);
            return Add(new FloatDefinition(name, label, description, min, max, defaultValue));
        }

        /// <summary>
        /// Declares an integer parameter and returns its stable index.<br/>
        /// 정수 parameter를 선언하고 안정적인 index를 반환합니다.
        /// </summary>
        /// <param name="name">
        /// UTF-8 parameter name with at most 15 bytes.<br/>
        /// UTF-8 기준 최대 15 byte인 parameter 이름입니다.
        /// </param>
        /// <param name="min">
        /// Minimum accepted value.<br/>
        /// 허용할 최솟값입니다.
        /// </param>
        /// <param name="max">
        /// Maximum accepted value.<br/>
        /// 허용할 최댓값입니다.
        /// </param>
        /// <param name="defaultValue">
        /// Initial value within the declared range.<br/>
        /// 선언한 범위 안의 초기값입니다.
        /// </param>
        /// <param name="label">
        /// Optional UTF-8 unit label with at most 15 bytes.<br/>
        /// UTF-8 기준 최대 15 byte인 선택적 단위 label입니다.
        /// </param>
        /// <param name="description">
        /// Optional parameter description.<br/>
        /// 선택적 parameter 설명입니다.
        /// </param>
        /// <returns>
        /// Stable parameter index.<br/>
        /// 안정적인 parameter index를 반환합니다.
        /// </returns>
        public int AddInt(string name, int min, int max, int defaultValue, string label = "", string description = "")
        {
            ValidateRange(name, min, max, defaultValue);
            ValidateMetadata(label, description);
            return Add(new IntDefinition(name, label, description, min, max, defaultValue));
        }

        /// <summary>
        /// Declares a boolean parameter and returns its stable index.<br/>
        /// Boolean parameter를 선언하고 안정적인 index를 반환합니다.
        /// </summary>
        /// <param name="name">
        /// UTF-8 parameter name with at most 15 bytes.<br/>
        /// UTF-8 기준 최대 15 byte인 parameter 이름입니다.
        /// </param>
        /// <param name="defaultValue">
        /// Initial value of the parameter.<br/>
        /// parameter의 초기값입니다.
        /// </param>
        /// <param name="label">
        /// Optional UTF-8 unit label with at most 15 bytes.<br/>
        /// UTF-8 기준 최대 15 byte인 선택적 단위 label입니다.
        /// </param>
        /// <param name="description">
        /// Optional parameter description.<br/>
        /// 선택적 parameter 설명입니다.
        /// </param>
        /// <returns>
        /// Stable parameter index.<br/>
        /// 안정적인 parameter index를 반환합니다.
        /// </returns>
        public int AddBool(string name, bool defaultValue, string label = "", string description = "")
        {
            ValidateText(name, nameof(name), required: true);
            ValidateMetadata(label, description);
            return Add(new BoolDefinition(name, label, description, defaultValue));
        }

        int Add(Definition definition)
        {
            if (isSealed)
                throw new InvalidOperationException("Custom DSP parameters can only be declared during OnConfigureParameters.");

            definitions.Add(definition);
            return definitions.Count - 1;
        }

        internal CustomDSPParameterStorage BuildStorage()
        {
            Seal();
            return new CustomDSPParameterStorage(definitions);
        }

        internal void Seal() => isSealed = true;

        static void ValidateRange<T>(string name, T min, T max, T defaultValue) where T : IComparable<T>
        {
            ValidateText(name, nameof(name), required: true);
            if (min.CompareTo(max) > 0)
                throw new ArgumentException("The minimum value must not exceed the maximum value.", nameof(min));
            if (defaultValue.CompareTo(min) < 0 || defaultValue.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(nameof(defaultValue), "The default value must be within the declared range.");
        }

        static void ValidateText(string value, string parameterName, bool required)
        {
            ExceptionUtility.ThrowIfArgumentNull(value, parameterName);
            if (required && string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("The parameter name must not be empty.", parameterName);
            if (Encoding.UTF8.GetByteCount(value) >= 16)
                throw new ArgumentException("The UTF-8 text must fit in 15 bytes.", parameterName);
        }

        static void ValidateMetadata(string label, string description)
        {
            ValidateText(label, nameof(label), required: false);
            ExceptionUtility.ThrowIfArgumentNull(description, nameof(description));
        }

        internal abstract class Definition
        {
            protected Definition(string name, string label, string description)
            {
                this.name = name;
                this.label = label;
                this.description = description;
            }

            readonly string name;
            readonly string label;
            readonly string description;

            protected DSP_PARAMETER_DESC CreateBaseDescription(DSP_PARAMETER_TYPE type) => new()
            {
                type = type,
                name = EncodeFixed(name),
                label = EncodeFixed(label),
                description = description,
            };

            public abstract DSP_PARAMETER_DESC CreateDescription();
        }

        sealed class FloatDefinition : Definition
        {
            readonly float min;
            readonly float max;
            readonly float defaultValue;

            public FloatDefinition(string name, string label, string description, float min, float max, float defaultValue) : base(name, label, description)
            {
                this.min = min;
                this.max = max;
                this.defaultValue = defaultValue;
            }

            public override DSP_PARAMETER_DESC CreateDescription()
            {
                DSP_PARAMETER_DESC description = CreateBaseDescription(DSP_PARAMETER_TYPE.FLOAT);
                description.desc.floatdesc = new DSP_PARAMETER_DESC_FLOAT
                {
                    min = min,
                    max = max,
                    defaultval = defaultValue,
                    mapping = new DSP_PARAMETER_FLOAT_MAPPING
                    {
                        type = DSP_PARAMETER_FLOAT_MAPPING_TYPE.DSP_PARAMETER_FLOAT_MAPPING_TYPE_LINEAR,
                    },
                };
                return description;
            }
        }

        sealed class IntDefinition : Definition
        {
            readonly int min;
            readonly int max;
            readonly int defaultValue;

            public IntDefinition(string name, string label, string description, int min, int max, int defaultValue) : base(name, label, description)
            {
                this.min = min;
                this.max = max;
                this.defaultValue = defaultValue;
            }

            public override DSP_PARAMETER_DESC CreateDescription()
            {
                DSP_PARAMETER_DESC description = CreateBaseDescription(DSP_PARAMETER_TYPE.INT);
                description.desc.intdesc = new DSP_PARAMETER_DESC_INT
                {
                    min = min,
                    max = max,
                    defaultval = defaultValue,
                };
                return description;
            }
        }

        sealed class BoolDefinition : Definition
        {
            readonly bool defaultValue;

            public BoolDefinition(string name, string label, string description, bool defaultValue) : base(name, label, description) => this.defaultValue = defaultValue;

            public override DSP_PARAMETER_DESC CreateDescription()
            {
                DSP_PARAMETER_DESC description = CreateBaseDescription(DSP_PARAMETER_TYPE.BOOL);
                description.desc.booldesc = new DSP_PARAMETER_DESC_BOOL
                {
                    defaultval = defaultValue,
                };
                return description;
            }
        }

        static byte[] EncodeFixed(string value)
        {
            byte[] result = new byte[16];
            Encoding.UTF8.GetBytes(value, 0, value.Length, result, 0);
            return result;
        }
    }

    [Obsolete("CustomDSP has not been tested and is quite complex!")]
    sealed class CustomDSPParameterStorage : IDisposable
    {
        readonly List<IntPtr> descriptorPointers = [];

        public CustomDSPParameterStorage(IReadOnlyList<CustomDSPParameterBuilder.Definition> definitions)
        {
            count = definitions.Count;
            if (count == 0)
                return;

            try
            {
                pointerArray = Marshal.AllocHGlobal(checked(IntPtr.Size * count));
                int descriptorSize = Marshal.SizeOf<DSP_PARAMETER_DESC>();

                for (int index = 0; index < count; index++)
                {
                    IntPtr descriptorPointer = Marshal.AllocHGlobal(descriptorSize);
                    Marshal.StructureToPtr(definitions[index].CreateDescription(), descriptorPointer, false);
                    descriptorPointers.Add(descriptorPointer);
                    Marshal.WriteIntPtr(pointerArray, IntPtr.Size * index, descriptorPointer);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public int count { get; }
        public IntPtr pointerArray { get; private set; }

        public void Dispose()
        {
            foreach (IntPtr descriptorPointer in descriptorPointers)
            {
                Marshal.DestroyStructure<DSP_PARAMETER_DESC>(descriptorPointer);
                Marshal.FreeHGlobal(descriptorPointer);
            }

            descriptorPointers.Clear();

            if (pointerArray == IntPtr.Zero)
                return;

            Marshal.FreeHGlobal(pointerArray);
            pointerArray = IntPtr.Zero;
        }
    }
}

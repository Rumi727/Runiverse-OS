#nullable enable
namespace RuniOS.Editor.UIElements
{
    /// <summary>
    /// 키-값 쌍을 나타내는 복합 필드입니다.
    /// </summary>
    /// <typeparam name="TPair">이 필드가 나타내는 키-값 쌍 타입입니다.</typeparam>
    public class KeyValuePairField<TPair> : RuniBaseCompositeField<TPair>
    {
        public new const string ussClassName = "runios-key-value-pair-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// 키 필드에 대한 설명을 담고 있는 <see cref="RuniBaseCompositeField{TValueType}.IElementDescription"/>입니다.
        /// </summary>
        public IElementDescription keyDescription { get; }
        /// <summary>
        /// 값 필드에 대한 설명을 담고 있는 <see cref="RuniBaseCompositeField{TValueType}.IElementDescription"/>입니다.
        /// </summary>
        public IElementDescription valueDescription { get; }
        
        /// <summary>
        /// <see cref="KeyValuePairField{TPair}"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="keyDescription">키 필드에 대한 설명 데이터입니다.</param>
        /// <param name="valueDescription">값 필드에 대한 설명 데이터입니다.</param>
        public KeyValuePairField(IElementDescription keyDescription, IElementDescription valueDescription) : this(string.Empty, keyDescription, valueDescription) { }
        
        /// <summary>
        /// <see cref="KeyValuePairField{TPair}"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="label">필드에 표시될 라벨입니다.</param>
        /// <param name="keyDescription">키 필드에 대한 설명 데이터입니다.</param>
        /// <param name="valueDescription">값 필드에 대한 설명 데이터입니다.</param>
        public KeyValuePairField(string label, IElementDescription keyDescription, IElementDescription valueDescription) : base(label)
        {
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            this.keyDescription = keyDescription;
            this.valueDescription = valueDescription;
            
            AddToClassList(ussClassName);
            SetFieldsByHorizontal();
        }

        protected override IEnumerable<IElementDescription> GetElementDescriptions()
        {
            yield return keyDescription;
            yield return GetSpacer();
            yield return valueDescription;
        }
    }
}
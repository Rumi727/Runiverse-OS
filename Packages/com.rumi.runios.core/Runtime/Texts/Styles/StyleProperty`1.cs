#nullable enable
namespace RuniOS.Texts.Styles
{
    /// <summary>
    /// Represents an explicitly set or unset optional value.<br/>
    /// 명시적으로 설정되었거나 설정되지 않은 optional 값을 나타냅니다.
    /// </summary>
    /// <typeparam name="T">
    /// The stored value type.<br/>
    /// 저장되는 값 타입입니다.
    /// </typeparam>
    /// <param name="value">
    /// The value to store.<br/>
    /// 저장할 값입니다.
    /// </param>
    [Serializable]
    public struct StyleProperty<T>(T value)
    {
        /// <summary>
        /// Gets or sets a value indicating whether this optional contains a value.<br/>
        /// 이 optional이 값을 포함하는지 여부를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public bool hasValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the stored value.<br/>
        /// 저장된 값을 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public T value { get; set; } = value;

        /// <summary>
        /// Creates an optional value from the specified value.<br/>
        /// 지정된 값에서 optional 값을 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.<br/>
        /// 감쌀 값입니다.
        /// </param>
        public static implicit operator StyleProperty<T>(T value) => new StyleProperty<T>(value);
    }
}

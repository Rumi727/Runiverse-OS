using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RuniOS
{
    /// <summary>
    /// Represents a key-value pair used to replace a placeholder in a localized string with a dynamic value.<br/>
    /// The <see cref="oldValue"/> acts as the placeholder key (e.g., "value") and the <see cref="newValue"/> is the replacement text.
    /// <br/><br/>
    /// 지역화된 문자열에서 플레이스홀더를 동적 값으로 대체하는 데 사용되는 키-값 쌍을 나타냅니다.<br/>
    /// <see cref="oldValue"/>는 플레이스홀더 키 (예: "value") 역할을 하며 <see cref="newValue"/>는 대체 텍스트입니다.
    /// </summary>
    [Serializable]
    public struct PlaceholderReplacePair : IEquatable<PlaceholderReplacePair>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderReplacePair"/> struct.<br/>
        /// 새 <see cref="PlaceholderReplacePair"/> 구조체의 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="oldValue">
        /// The placeholder key (e.g., "name") to be replaced in the localized text.<br/>
        /// 지역화된 텍스트에서 대체될 플레이스홀더 키 (예: "name").)
        /// </param>
        /// <param name="newValue">
        /// The value to replace the placeholder key with.<br/>
        /// 플레이스홀더 키를 대체할 값.
        /// </param>
        public PlaceholderReplacePair(string oldValue, string newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }
        
        [SerializeField, JsonIgnore, FieldName("gui.replace.old")] string? _oldValue;
        [SerializeField, JsonIgnore, FieldName("gui.replace.new")] string? _newValue;
        
        /// <summary>
        /// Gets or sets the placeholder key. This value is used to match and replace text enclosed in braces (e.g., it matches "name" to "{name}").<br/>
        /// 플레이스홀더 키를 가져오거나 설정합니다. 이 값은 중괄호로 묶인 텍스트를 일치 및 대체하는 데 사용됩니다 (예: "name"을 "{name}"에 일치).
        /// </summary>
        public string oldValue
        {
            readonly get => _oldValue ?? string.Empty;
            set => _oldValue = value;
        }
        
        /// <summary>
        /// Gets or sets the replacement value.<br/>
        /// 대체 값을 가져오거나 설정합니다.
        /// </summary>
        public string newValue
        {
            readonly get => _newValue ?? string.Empty;
            set => _newValue = value;
        }
        
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.<br/>
        /// 현재 객체가 같은 형식의 다른 객체와 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.<br/>
        /// 이 객체와 비교할 객체.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the current object is equal to the <paramref name="other"/>; otherwise, <see langword="false"/>.<br/>
        /// 현재 객체가 <paramref name="other"/>와 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.)
        /// </returns>
        public readonly bool Equals(PlaceholderReplacePair other) => oldValue == other.oldValue && newValue == other.newValue;
        
        public override readonly bool Equals(object? obj) => obj is PlaceholderReplacePair other && Equals(other);
        
        public override readonly int GetHashCode() => HashCode.Combine(oldValue, newValue);

        /// <summary>
        /// Replaces the placeholder key enclosed in braces (e.g., "{oldValue}") with the <see cref="newValue"/> in the provided text.<br/>
        /// This ensures replacement only occurs on explicitly defined placeholders, avoiding accidental cascading replacement.
        /// <br/><br/>
        /// 중괄호로 묶인 플레이스홀더 키(예: "{oldValue}")를 제공된 텍스트에서 <see cref="newValue"/>로 대체합니다.<br/>
        /// 이는 명시적으로 정의된 플레이스홀더에 대해서만 대체가 발생하도록 보장하여 의도하지 않은 연쇄 대체를 방지합니다.
        /// </summary>
        /// <param name="text">
        /// The text containing the placeholder to be replaced.<br/>
        /// 대체될 플레이스홀더가 포함된 텍스트.
        /// </param>
        /// <returns>
        /// The text with the replacement applied.<br/>
        /// 대체가 적용된 텍스트.
        /// </returns>
        public readonly string ReplaceAsPlaceholder(string text) => text.Replace($"{{{oldValue}}}", newValue);
        
        /// <summary>
        /// Compares two <see cref="PlaceholderReplacePair"/> instances for equality.<br/>
        /// 두 <see cref="PlaceholderReplacePair"/> 인스턴스의 같음(equality)을 비교합니다.
        /// </summary>
        /// <param name="left">
        /// The first instance.<br/>
        /// 첫 번째 인스턴스.
        /// </param>
        /// <param name="right">
        /// The second instance.<br/>
        /// 두 번째 인스턴스.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.<br/>
        /// 인스턴스가 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.
        /// </returns>
        public static bool operator ==(PlaceholderReplacePair left, PlaceholderReplacePair right) => left.Equals(right);
        
        /// <summary>
        /// Compares two <see cref="PlaceholderReplacePair"/> instances for inequality.<br/>
        /// 두 <see cref="PlaceholderReplacePair"/> 인스턴스의 다름(inequality)을 비교합니다.
        /// </summary>
        /// <param name="left">
        /// The first instance.<br/>
        /// 첫 번째 인스턴스.
        /// </param>
        /// <param name="right">
        /// The second instance.<br/>
        /// 두 번째 인스턴스.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.<br/>
        /// 인스턴스가 같지 않으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.
        /// </returns>
        public static bool operator !=(PlaceholderReplacePair left, PlaceholderReplacePair right) => !left.Equals(right);
    }
}